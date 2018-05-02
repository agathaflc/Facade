# imports
import cv2
import numpy as np
import operator
import time
import json
import os
from load_model_emotion import load_model
from inference import detect_faces, draw_text, draw_bounding_box, apply_offsets, load_detection_model

def preprocess_input(x, v2=True):
    
    x = np.resize(x, (48,48))
    x = x.reshape(1, 1, 48, 48)
    x = x.astype('float32')
    x = x / 255.0
    if v2:
        x = x - 0.5
        x = x * 2.0
        
    return x

# parameters for loading data and images
detection_model_path = 'haarcascade_frontalface_default.xml'
path_inputs = 'saved_models/deployment/model.json'
path_weights = 'saved_models/deployment/best_weights.hd5'
emotion_labels = {0:'neutral',1:'anger',2:'fear',3:'happy',4:'sad',5:'surprise'}

# loading models
face_detection = load_detection_model(detection_model_path)
emotion_classifier = load_model(path_inputs=path_inputs, path_weights=path_weights)

def read_expression():
    
    global emotion_labels
    # hyper-parameters for bounding boxes shape
    frame_window = 5
    emotion_offsets = (0, 0)

    # starting lists for calculating modes
    emotion_window = []

    # starting video streaming
    cv2.namedWindow('window_frame', 0)
    cv2.resizeWindow('window_frame', 500, 500)
    video_capture = cv2.VideoCapture(0)
    video_capture.set(cv2.CAP_PROP_BRIGHTNESS, 50)

    timeout = time.time() + 2.3 # 2.8 seconds from now
    sleep_index = 0

    # initialise dictionary of emotion confidences
    emotion_confidences = {'neutral': 0.0, 'anger': 0.0, 'fear': 0.0, 'happy': 0.0, 'sad': 0.0, 'surprise': 0.0}
    c = 0
    while True:
        if time.time() > timeout:
            break

        bgr_image = video_capture.read()[1]
        gray_image = cv2.cvtColor(bgr_image, cv2.COLOR_BGR2GRAY)
        rgb_image = cv2.cvtColor(bgr_image, cv2.COLOR_BGR2RGB)
        faces = detect_faces(face_detection, gray_image)

        for face_coordinates in faces:

            x1, x2, y1, y2 = apply_offsets(face_coordinates, emotion_offsets)
            gray_face = gray_image[y1:y2, x1:x2]
            print("gray face shape original", gray_face.shape)

            gray_face = preprocess_input(gray_face, False)
            print("gray face shape post-preprocessing", gray_face.shape)

            emotion_prediction = emotion_classifier.predict(gray_face)
            pred_class = emotion_classifier.predict_classes(gray_face)
            print ("emotion prediction", emotion_prediction)
            emotion_probability = np.max(emotion_prediction)
            print ("emotion probability", emotion_probability)
            # use this info for printing model output
            emotion_label_arg = np.argmax(emotion_prediction)
            print("ARG", emotion_label_arg)
            emotion_text = emotion_labels[emotion_label_arg]
            emotion_confidences[emotion_text] += np.amax(emotion_prediction)

            print("EMOTION:", emotion_text)
            print("PROB:", emotion_probability.max())
            c += 1

            emotion_scores = emotion_prediction.tolist()[0]
            emotion_array = {}

            for counter, value in enumerate(emotion_scores):
                new_item = {emotion_labels[counter]:value}
                emotion_array.update(new_item)

            sorted_x = sorted(emotion_array.items(), key=operator.itemgetter(1), reverse=True)
            first = emotion_labels[pred_class.tolist()[0]]
            first_score = emotion_array[first]
            second = sorted_x[1][0]
            second_score=emotion_array[second]


            #second_emotion

            # logic -- append label to emotion_window (global array)
            emotion_window.append(emotion_text)
            print ("emotion window", emotion_window)

            # logic -- if more than 10 emotions in emotion_window, reset the window
            if len(emotion_window) > frame_window:
                emotion_window.pop(0)

            if emotion_text == 'angry':
                color = emotion_probability * np.asarray((255, 0, 0)) # red
            elif emotion_text == 'sad':
                color = emotion_probability * np.asarray((0, 0, 255)) # blue
            elif emotion_text == 'happy':
                color = emotion_probability * np.asarray((255, 255, 0)) # red and green
            elif emotion_text == 'surprise':
                color = emotion_probability * np.asarray((0, 255, 255)) # blue green
            # fear & neutral
            else:
                color = emotion_probability * np.asarray((0, 255, 0))

            color = color.astype(int)
            color = color.tolist()
            print ("color", color)

            draw_bounding_box(face_coordinates, rgb_image, color)
            draw_text(face_coordinates, rgb_image, emotion_text+'  '+'%s:%.3f  %s:%.3f' % (first, first_score, second, second_score),
                      color, 0, -45, 1, 1)
            # make sure loop isn't hogging cpu
            sleep_index += 1
            if sleep_index % 5 == 0:
                time.sleep(1)

        bgr_image = cv2.cvtColor(rgb_image, cv2.COLOR_RGB2BGR)
        cv2.imshow('window_frame', bgr_image)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break
        
        print("COUNT:",c)

    adj2noun = {'neutral': 'neutral', 'anger': 'angry', 'fear': 'scared', 'happy': 'happy', 'surprise': 'surprised', 'sad': 'sad'}

    print(dict(sorted(emotion_confidences.items(), key=operator.itemgetter(1), reverse=True)))
    print(dict(sorted(emotion_confidences.items(), key=operator.itemgetter(1), reverse=True)[:3]))
    write_output_to_file(emotion_confidences, c, adj2noun)

def write_output_to_file(emotion_conf, max_val, adj2noun):
    output_dict = dict(sorted(emotion_conf.items(), key=operator.itemgetter(1), reverse=True)[:3])
    for key in output_dict:
        if (max_val != 0):
            output_dict[key] = output_dict[key] / max_val
        else:
            output_dict[key] = 0

    final_output_list = []
    for key in output_dict:
        temp = {}
        temp['emotion'] = adj2noun[key]
        temp['emotionScore'] = output_dict[key]
        final_output_list.append(temp)

    final_emotions_output = {}
    final_emotions_output['emotions'] = final_output_list

    with open(os.path.join('..', 'expression_data.json'), 'w') as fp:
        json.dump(final_emotions_output, fp)

def check_trigger():
    try:
        with open(os.path.join('..', 'flag.txt'), 'r') as fp:
            if (fp.read()) == "record":
                print("RECORDING")
                return True
            else:
                return False
    except Exception as e:
        print(e)
        print("Exiting")
        exit(1)

def main():
    while True:
        if check_trigger():
            read_expression()

        time.sleep(0.2) # checks every 0.2 seconds

main()
