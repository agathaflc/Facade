# imports
import sys
import args
import os
import numpy as np
import glob
import cv2
import matplotlib.pyplot as plt
import matplotlib
import brewer2mpl
import pandas as pd
from utils.inference import detect_faces, apply_offsets, load_detection_model
from keras.utils import np_utils
print("imports successful")

# fer2013 dataset:
# Training       28709
# PrivateTest     3589
# PublicTest      3589

# emotion labels from FER2013:
emotion_old = {'Angry': 0, 'Disgust': 1, 'Fear': 2, 'Happy': 3,
           'Sad': 4, 'Surprise': 5, 'Neutral': 6}

# emotion labels reconstructed (absorbed disgust into anger):
emotion_new = {'Neutral': 0, 'Anger': 1, 'Fear': 2, 'Happy': 3,
           'Sad': 4, 'Surprise': 5}

def save_processed_data(name, data):
    np.save(name, data)

def load_processed_data(outfile):
    d = np.load(outfile)
    return d

def extract_face(pixel_array):

    return_image = pixel_array

    image=(np.array(pixel_array, dtype='uint8')).reshape(1,1,48,48)[0,0,:,:]

    emotion_offsets = (0, 0)
    faces=None

    if faces==None:
        face_detection = load_detection_model('haarcascade_frontalface_default.xml')
        faces = detect_faces(face_detection, image)
    print(faces)
    if len(faces)==0:
        face_detection = load_detection_model('haarcascade_frontalface_alt2.xml')
        faces = detect_faces(face_detection, image)
    print(faces)
    if len(faces)==0:
        face_detection = load_detection_model('haarcascade_profileface.xml')
        faces = detect_faces(face_detection, image)
    print(faces)
    if len(faces)==0:
        print("face not detected".upper())
        raise AssertionError

    for face_coordinates in faces:
        try:
            x1, x2, y1, y2 = apply_offsets(face_coordinates, emotion_offsets)
            image = image[y1:y2, x1:x2]
            image=np.resize(image, (48,48))
            return_image = np.array([image.flatten()])
            print ("shape: ",str(return_image.shape))
            print return_image
            return return_image
        except Exception as e:
            print("something went wrong".upper())
            raise e
	break

def process_inputs(filename="../../fer2013/fer2013.csv", extract_bool=False):

    # training attr (x) & labels (y)
    X_train=np.array([])
    Y_train=np.array([])

    # testing attr (x) & labels (y)
    X_test =np.array([])
    Y_test=np.array([])

    # validating attr (x) & labels (y)
    X_validate =np.array([])
    Y_validate=np.array([])

    # read csv
    df = pd.read_csv(filename, header=None, sep='rows separator', engine = 'python', skiprows = 1)
    print ("csv read")

    # store csv row by row
    dataList=[]

    # looping through each row data
    for index, row in df.iterrows():

        # extract the row
        datarow = row.loc[0].split(",")
        dataList.append(datarow)

        # extract columns of the row
        label_init = datarow[0]

        if label_init=='6':
            print("edited 6 to 0")
            label_init=0
        elif label_init=='0':
            print("edited 0 to 1")
            label_init=1

        print(label_init)
        label=np.array([np.array(label_init)])
        image=np.array([np.array([int(i) for i in datarow[1].split(" ")])])
        print(image)
        if extract_bool:
            try:
                image=extract_face(image)
            except Exception as e:
                print("ignoring image input: {}".format(e))
                continue
                pass
        purpose=datarow[2]

        # populate dataset
        try:
            if purpose == 'Training':
                X_train=np.append(X_train, image, axis=0)
                Y_train=np.append(Y_train, label, axis=0)
                print("training bingo")
            elif 'private' in purpose.lower():
                X_test=np.append(X_test, image, axis=0)
                print("testing x")
                Y_test=np.append(Y_test, label, axis=0)
                print("testing y")
                print (X_test, Y_test)
            else:
                X_validate=np.append(X_validate, image, axis=0)
                Y_validate=np.append(Y_validate, label, axis=0)

        except Exception as e:
            print ("missed: {}".format(e))
            if purpose == 'Training':
                X_train=np.array(image)
                Y_train=np.array(label)
            elif 'private' in purpose.lower():
                X_test=np.array(image)
                Y_test=np.array(label)
                print (X_test, Y_test)
            else:
                X_validate=np.array(image)
                Y_validate=np.array(label)

    # Preprocess input data

    X_train = X_train.reshape(X_train.shape[0], 1, 48, 48)
    X_validate = X_validate.reshape(X_validate.shape[0], 1, 48, 48)
    X_test = X_test.reshape(X_test.shape[0], 1, 48, 48)

    X_train = X_train.astype('float32')
    X_validate = X_validate.astype('float32')
    X_test = X_test.astype('float32')

    X_train /= 255
    X_validate /= 255
    X_test /= 255

    print ("Preprocessed input data")

    # Preprocess class labels
    Y_train = np_utils.to_categorical(Y_train, 6)
    Y_test = np_utils.to_categorical(Y_test, 6)
    Y_validate = np_utils.to_categorical(Y_validate, 6)

    print ("Preprocessed class labels")

    return (X_train, Y_train), (X_test, Y_test), (X_validate, Y_validate)

def load_save_inputs(process, filename="../../fer2013/fer2013.csv", saved_inputs="saved_inputs"):

    # optional saving of inputs
    if process:

        (X_train, Y_train), (X_test, Y_test), (X_validate, Y_validate) = process_inputs(filename, True)

        save_processed_data(saved_inputs+"/X_train.npy", X_train)
        save_processed_data(saved_inputs+"/X_validate.npy", X_validate)
        save_processed_data(saved_inputs+"/X_test.npy", X_test)
        save_processed_data(saved_inputs+"/Y_train.npy", Y_train)
        save_processed_data(saved_inputs+"/Y_test.npy", Y_test)
        save_processed_data(saved_inputs+"/Y_validate.npy",Y_validate)
        print("saved")

    # loading of inputs
    X_train=load_processed_data(saved_inputs+"/X_train.npy")
    X_validate=load_processed_data(saved_inputs+"/X_validate.npy")
    X_test=load_processed_data(saved_inputs+"/X_test.npy")
    Y_train=load_processed_data(saved_inputs+"/Y_train.npy")
    Y_test=load_processed_data(saved_inputs+"/Y_test.npy")
    Y_validate=load_processed_data(saved_inputs+"/Y_validate.npy")
    print("loaded")

    print(np.all(X_train==load_processed_data(saved_inputs+"/X_train.npy")))
    print(np.all(X_validate==load_processed_data(saved_inputs+"/X_validate.npy")))
    print(np.all(X_test==load_processed_data(saved_inputs+"/X_test.npy")))
    print(np.all(Y_train==load_processed_data(saved_inputs+"/Y_train.npy")))
    print(np.all(Y_test==load_processed_data(saved_inputs+"/Y_test.npy")))
    print(np.all(Y_validate==load_processed_data(saved_inputs+"/Y_validate.npy")))
    print("compared")


    return (X_train, Y_train), (X_test, Y_test), (X_validate, Y_validate)


if __name__ == '__main__':
    load_save_inputs(True, saved_inputs='saved_inputs_face_only')
