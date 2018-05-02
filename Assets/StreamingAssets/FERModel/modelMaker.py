
# imports
import sys
import args
req_link = '/usr/local/lib/python2.7/site-packages/'
sys.path.append(req_link)
import os
from keras import backend as K
K.set_image_dim_ordering('th')
from sklearn.metrics import classification_report
import numpy as np
np.random.seed(123)  # for reproducibility
from keras import metrics
from keras.callbacks import EarlyStopping, TensorBoard
from keras.models import Sequential
from keras.layers import Dense, Dropout, Activation, Flatten
from keras.layers import Conv2D, MaxPooling2D, AveragePooling2D
from keras.utils import np_utils
from keras.datasets import mnist
from keras.models import model_from_json
import glob
import cv2
from keras.preprocessing.image import ImageDataGenerator
import matplotlib.pyplot as plt
import matplotlib
import brewer2mpl
import pandas as pd
from pprint import pprint
set3 = brewer2mpl.get_map('Set3', 'qualitative', 6).mpl_colors
print("imports successful")

# emotion labels from FER2013:
emotion_old = {'Angry': 0, 'Disgust': 1, 'Fear': 2, 'Happy': 3,
           'Sad': 4, 'Surprise': 5, 'Neutral': 6}

# emotion labels reconstructed (absorbed disgust into anger):
emotion_new = {'Neutral': 0, 'Anger': 1, 'Fear': 2, 'Happy': 3,
           'Sad': 4, 'Surprise': 5}

def loadFER():

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
    filename = "../../fer2013/fer2013.csv"
    df = pd.read_csv(filename, header=None, sep='rows separator', engine = 'python', skiprows = 1, nrows=300)
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

(X_train, Y_train), (X_test, Y_test), (X_validate, Y_validate) = loadFER()

def save_processed_data(name, data):
    np.save(name, data)

def load_processed_data(outfile):
    d = np.load(outfile)
    return d

save_processed_data("X_validate.npy", X_validate)
save_processed_data("Y_validate.npy",Y_validate)
print("saved")
X_train=load_processed_data("X_train.npy")
X_validate=load_processed_data("X_validate.npy")
X_test=load_processed_data("X_test.npy")
Y_train=load_processed_data("Y_train.npy")
Y_test=load_processed_data("Y_test.npy")
Y_validate=load_processed_data("Y_validate.npy")
print("loaded")

def hidden():
           
    try:
        print(np.all(X_train==load_processed_data("X_train.npy")))
        print(np.all(X_validate==load_processed_data("X_validate.npy")))
        print(np.all(X_test==load_processed_data("X_test.npy")))
        print(np.all(Y_train==load_processed_data("Y_train.npy")))
        print(np.all(Y_test==load_processed_data("Y_test.npy")))
        print(np.all(Y_validate==load_processed_data("Y_validate.npy")))
        print("compared")
    except NameError:
        X_train=load_processed_data("X_train.npy")
        X_validate=load_processed_data("X_validate.npy")
        X_test=load_processed_data("X_test.npy")
        Y_train=load_processed_data("Y_train.npy")
        Y_test=load_processed_data("Y_test.npy")
        Y_validate=load_processed_data("Y_validate.npy")
        print("loaded")
    except:
        save_processed_data("X_train.npy", X_train)
        save_processed_data("X_validate.npy", X_validate)
        save_processed_data("X_test.npy", X_test)
        save_processed_data("Y_train.npy", Y_train)
        save_processed_data("Y_test.npy", Y_test)
        save_processed_data("Y_validate.npy",Y_validate)
        print("saved")
        print(np.all(X_train==load_processed_data("X_train.npy")))
        print(np.all(X_validate==load_processed_data("X_validate.npy")))
        print(np.all(X_test==load_processed_data("X_test.npy")))
        print(np.all(Y_train==load_processed_data("Y_train.npy")))
        print(np.all(Y_test==load_processed_data("Y_test.npy")))
        print(np.all(Y_validate==load_processed_data("Y_validate.npy")))

def train_model(X_train, Y_train, X_test, Y_test):

    # Define model architecture
    model = Sequential()

    model.add(Conv2D(32, (2, 2), padding='same', activation='relu', input_shape=(1, X_train.shape[2], X_train.shape[3])))
    model.add(Conv2D(32, (2, 2), padding='same', activation='relu'))
    model.add(Conv2D(32, (2, 2), activation='relu'))
    model.add(MaxPooling2D(pool_size=(2,2)))
    model.add(Dropout(0.5))

    model.add(Conv2D(64, (2, 2), padding='same', activation='relu'))
    model.add(Conv2D(64, (2, 2), activation='relu'))
    model.add(MaxPooling2D(pool_size=(2,2)))
    model.add(Dropout(0.5))

    model.add(Conv2D(128, (3, 3), padding='same', activation='relu'))
    model.add(Conv2D(128, (3, 3), activation='relu'))
    model.add(MaxPooling2D(pool_size=(2,2)))
    model.add(Dropout(0.5))

    model.add(Flatten())
    model.add(Dense(128, activation='relu'))
    model.add(Dropout(0.5))
    model.add(Dense(64, activation='relu'))
    model.add(Dense(32, activation='relu'))
    model.add(Dense(6, activation='softmax'))

    # Compile model
    model.compile(loss='categorical_crossentropy',
                  optimizer='adam',
                  metrics=['accuracy']) # weight , precision, recall, fscore for each class
    # precision at 2 --- p@n --- out of N values if correct -- then correct

    earlyStopping = EarlyStopping(monitor='val_loss', min_delta=0, patience=5, verbose=0, mode='auto')
    tensorBoard=TensorBoard(log_dir='./logs', histogram_freq=0, batch_size=32, write_graph=True, write_grads=False, write_images=False, embeddings_freq=0, embeddings_layer_names=None, embeddings_metadata=None)

    # Fit model on training data
    model.fit(X_train, Y_train, validation_data=(X_validate, Y_validate), callbacks=[earlyStopping,tensorBoard],
              batch_size=32, epochs=10, shuffle=True, verbose=1)

    # serialize model to JSON
    model_json = model.to_json()
    with open("data/results/model.json", "w") as json_file:
        json_file.write(model_json)

    # serialize weights to HDF5
    model.save_weights("data/results/model.h5", overwrite=True)
    print("Saved model to disk")
    print ("hi")
    return model

train_model = train_model(X_train, Y_train, X_test, Y_test)

def load_model():

    # load json and create model
    json_file = open('data/results/model.json', 'r')
    loadmodel = json_file.read()
    json_file.close()
    model = model_from_json(loadmodel)

    # load weights into new model
    model.load_weights("data/results/model.h5")
    print("Loaded model from disk")

    # Compile model
    model.compile(loss='categorical_crossentropy',
                  optimizer='adam',
                  metrics=['accuracy'])

    print("Compiled loaded model")

    return model

def eval_model(model, X_test, Y_test):

    # Evaluate model on test data
    score = model.evaluate(X_test, Y_test, verbose=0)
    print(X_test, Y_test)
    print score
    print('Test loss:', score[0])
    print('Test accuracy:', score[1])
           
    return model

model = load_model()

print("eval_model")
eval_model(model, X_test, Y_test)

def processImage(path):

    x = cv2.imread(path)
    x = cv2.cvtColor(x, cv2.COLOR_BGR2GRAY)
    x = cv2.resize(x,(48, 48))
    x = x.reshape(1, 1, 48, 48)
    x = x.astype('float32')
    x /= 255
           
    return x


def getLabel(label):

    pattern = ([0]*label)+[1]+([0]*(5-label))
    print pattern
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

model = load_model()
pprint (predict(select("happy"), 3, model))

yt = np.argmax(Y_test, axis=1) # Convert one-hot to index
y_pred = model.predict_classes(X_test)
print(classification_report(yt, y_pred))
