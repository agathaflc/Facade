import sys
import args
import os
import numpy as np
import cv2
from utils.inference import detect_faces, draw_text, draw_bounding_box, apply_offsets, load_detection_model
import operator

# parameters for loading data and images
detection_model_path = 'haarcascade_frontalface_default.xml'
emotion_labels = {0:'neutral',1:'anger',2:'fear',3:'happy',4:'sad',5:'surprise',6:'disgust',7:'contempt'}
emotion_offsets = (20, 40)

def processImage(path):

    image = cv2.imread(path,0)

    face_detection = load_detection_model(detection_model_path)
    faces = detect_faces(face_detection, image)
    first_face=""

    for face_coordinates in faces:
        first_face=face_coordinates
        break

    x1, x2, y1, y2 = apply_offsets(first_face, emotion_offsets)

    image = image[y1:y2, x1:x2]

    image = np.resize(image,(48, 48))

    image = image.reshape(1, 1, 48, 48)
    image = image.astype('float32')
    image /= 255
    return image


def getLabel(label):

    pattern = ([0]*label)+[1]+([0]*(5-label))
    print (pattern)
    label = np.array(pattern)

    label = label.reshape(1, 6)
    return label

def predict(image, label, model):

    label = getLabel(label)

    score = model.evaluate(image, label, verbose=0)
    pred_array = model.predict(image)
    pred_class = model.predict_classes(image)

    prediction = {"loss":score[0], "accuracy":score[1], "pred_array":pred_array, "pred_class": pred_class}

    return prediction

def select(path):
    try:
        x = processImage(str(path)+".png")
    except:
        x = processImage(str(path)+".jpeg")
    return x

def predict_emotion(image, model):

    pred_array = model.predict(image)
    pred_class = model.predict_classes(image)

    prediction = {"pred_array":pred_array, "pred_class": pred_class}
    print (prediction)
    return prediction


def predict_for_kate(image_or, model):

    image_or = cv2.imread(image_or,0)

    face_detection = load_detection_model(detection_model_path)
    faces = detect_faces(face_detection, image_or)
    face_array={}
    i=0

    for face_coordinates in faces:

        x1, x2, y1, y2 = apply_offsets(face_coordinates, emotion_offsets)

        image = image_or[y1:y2, x1:x2]

        image = np.resize(image,(48, 48))

        image = image.reshape(1, 1, 48, 48)
        image = image.astype('float32')
        image /= 255

        pred_array = model.predict(image)
        pred_class = model.predict_classes(image)
        prediction = {"pred_array":pred_array.tolist()[0], "pred_class": pred_class.tolist()[0], "coord": (x1, x2, y1, y2)}
        face_array.update({i:prediction})
        i+=1

    print (face_array)

    if not face_array:
        image = np.resize(image_or,(48, 48))
        image = image.reshape(1, 1, 48, 48)
        image = image.astype('float32')
        image /= 255

        pred_array = model.predict(image)
        pred_class = model.predict_classes(image)
        prediction = {"pred_array":pred_array.tolist()[0], "pred_class": pred_class.tolist()[0]}
        face_array.update({i:prediction})

    return face_array


def new_predict_for_kate(image_or, model):

    image_or = cv2.cvtColor(image_or, cv2.COLOR_RGB2GRAY)

    face_detection = load_detection_model(detection_model_path)
    faces = detect_faces(face_detection, image_or)
    face_array=[]

    for face_coordinates in faces:

        x1, x2, y1, y2 = apply_offsets(face_coordinates, emotion_offsets)

        image = image_or[y1:y2, x1:x2]

        image = np.resize(image,(48, 48))

        image = image.reshape(1, 1, 48, 48)
        image = image.astype('float32')
        image /= 255

        pred_array = model.predict(image)
        pred_class = model.predict_classes(image)

        emotion_scores = pred_array.tolist()[0]
        emotion_array = {}

        for counter, value in enumerate(emotion_scores):
            new_item = {emotion_labels[counter]:value}
            emotion_array.update(new_item)

        sorted_x = sorted(emotion_array.items(), key=operator.itemgetter(1), reverse=True)
        first = emotion_labels[pred_class.tolist()[0]]
        second = sorted_x[1][0]
        if sorted_x[1][1]<0.1:
            second='n/a'

        prediction = {"emotion":emotion_array, "prec_at_two": {"first": first, "second": second }, "faceRectangle": {"top":x1, "left":y1, "width":x2, "height":y2}}
        face_array.append(prediction)

    if not face_array:
        image = cv2.resize(image_or,(48, 48))
        image = image.reshape(1, 1, 48, 48)
        image = image.astype('float32')
        image /= 255

        pred_array = model.predict(image)
        pred_class = model.predict_classes(image)

        emotion_scores = pred_array.tolist()[0]
        emotion_array = {}

        for counter, value in enumerate(emotion_scores):
            new_item = {emotion_labels[counter]:value}
            emotion_array.update(new_item)

        sorted_x = sorted(emotion_array.items(), key=operator.itemgetter(1), reverse=True)
        first = emotion_labels[pred_class.tolist()[0]]
        second = sorted_x[1][0]
        if sorted_x[1][1]<0.1:
            second='n/a'

        prediction = {"emotion":emotion_array, "prec_at_two": {"first": first, "second": second }, "faceRectangle": 'not detected'}
        face_array.append(prediction)

    print (face_array)
    return face_array
