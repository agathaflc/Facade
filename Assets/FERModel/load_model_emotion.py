# coding: utf-8
# author: Akanksha Gupta
# usage: to load or evaluate models

import sys
import args

req_link = '/usr/local/lib/python2.7/site-packages/'
sys.path.append(req_link)

import os

from keras import backend as K
K.set_image_dim_ordering('th')

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
#import matplotlib.pyplot as plt
#import matplotlib
#import brewer2mpl
#import pandas as pd


def load_model(path_inputs="saved_models/multiclass_models/model.json", path_weights="saved_models/multiclass_models/model.h5"):
    
    # load json and create model
    json_file = open(path_inputs, 'r')
    loadmodel = json_file.read()
    json_file.close()
    model = model_from_json(loadmodel)

    # load weights into new model
    model.load_weights(path_weights)
    print("Loaded model from disk") 

    # compile model
    model.compile(loss='categorical_crossentropy',
                  optimizer='adam',
                  metrics=['accuracy'])

    print("Compiled loaded model") 
    return model


def eval_model(model, X_test, Y_test):
    score = model.evaluate(X_test, Y_test, verbose=0)
    # print(X_test, Y_test)
    print('Test loss:', score[0])
    print('Test accuracy:', score[1])
    return score
