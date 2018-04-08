# coding: utf-8
# author: Akanksha Gupta
# usage: to construct & train the CNN model (constructing layer by layer)

import sys
import args
import h5py
req_link = '/usr/local/lib/python2.7/site-packages/'
sys.path.append(req_link)

import os
os.environ['PYTHONHASHSEED'] = '0'

from keras import backend as K
K.set_image_dim_ordering('th')

import numpy as np
np.random.seed(123)  # for reproducibility

#import tensorflow as tf
#tf.set_random_seed(123)

import random as rn
rn.seed(123)

#session_conf = tf.ConfigProto(intra_op_parallelism_threads=1, inter_op_parallelism_threads=1)
#sess = tf.Session(graph=tf.get_default_graph(), config=session_conf)
#K.set_session(sess)

from keras import metrics
from keras.callbacks import ModelCheckpoint, EarlyStopping, TensorBoard, ReduceLROnPlateau
from keras.models import Sequential
from keras.layers import Dense, Dropout, Activation, Flatten, BatchNormalization, Input
from keras.layers import Conv2D, MaxPooling2D, AveragePooling2D, GlobalAveragePooling2D, GlobalMaxPooling2D, SeparableConv2D
from keras.regularizers import l2, l1_l2
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

print("imports successful")


def my_cnn(X_train, n_classes):

    # Define model architecture
    model = Sequential()

    model.add(Conv2D(32, (5, 5), padding='same', activation='relu', input_shape=(1, X_train.shape[2], X_train.shape[3])))
    model.add(Conv2D(32, (5, 5), padding='same', activation='relu'))
    model.add(AveragePooling2D(pool_size=(2,2)))
    model.add(Dropout(0.4))

    model.add(Conv2D(64, (3, 3), padding='same', activation='relu'))
    model.add(Conv2D(64, (3, 3), activation='relu'))
    model.add(AveragePooling2D(pool_size=(2,2)))
    
    model.add(Dropout(0.6))
    model.add(BatchNormalization())
    
    model.add(Conv2D(128, (3, 3), padding='same', activation='relu'))
    model.add(Conv2D(128, (3, 3), activation='relu'))
    model.add(AveragePooling2D(pool_size=(2,2)))
    model.add(Dropout(0.6))

    model.add(Conv2D (n_classes, (2, 2), padding='same', activation='relu'))
    model.add(Dropout(0.4))
    model.add(GlobalAveragePooling2D())
    model.add(Dense(n_classes,kernel_regularizer=l1_l2(0.01), activation='softmax'))
    model.summary()
    return model

def arriaga_mini_XCEPTION(input_shape, num_classes, l2_regularization=0.01):
    
    regularization = l2(l2_regularization)
    # base
    img_input = Input(input_shape)
    print (img_input)
    x = Conv2D(8, (3, 3), strides=(1, 1), kernel_regularizer=regularization,
                                            use_bias=False)(img_input)
    x = BatchNormalization()(x)
    x = Activation('relu')(x)
    x = Conv2D(8, (3, 3), strides=(1, 1), kernel_regularizer=regularization,
                                            use_bias=False)(x)
    x = BatchNormalization()(x)
    x = Activation('relu')(x)

    # module 1
    residual = Conv2D(16, (1, 1), strides=(2, 2),
                      padding='same', use_bias=False)(x)
    residual = BatchNormalization()(residual)

    x = SeparableConv2D(16, (3, 3), padding='same',
                        kernel_regularizer=regularization,
                        use_bias=False)(x)
    x = BatchNormalization()(x)
    x = Activation('relu')(x)
    x = SeparableConv2D(16, (3, 3), padding='same',
                        kernel_regularizer=regularization,
                        use_bias=False)(x)
    x = BatchNormalization()(x)

    x = MaxPooling2D((3, 3), strides=(2, 2), padding='same')(x)
    x = layers.add([x, residual])

    # module 2
    residual = Conv2D(32, (1, 1), strides=(2, 2),
                      padding='same', use_bias=False)(x)
    residual = BatchNormalization()(residual)

    x = SeparableConv2D(32, (3, 3), padding='same',
                        kernel_regularizer=regularization,
                        use_bias=False)(x)
    x = BatchNormalization()(x)
    x = Activation('relu')(x)
    x = SeparableConv2D(32, (3, 3), padding='same',
                        kernel_regularizer=regularization,
                        use_bias=False)(x)
    x = BatchNormalization()(x)

    x = MaxPooling2D((3, 3), strides=(2, 2), padding='same')(x)
    x = layers.add([x, residual])

    # module 3
    residual = Conv2D(64, (1, 1), strides=(2, 2),
                      padding='same', use_bias=False)(x)
    residual = BatchNormalization()(residual)

    x = SeparableConv2D(64, (3, 3), padding='same',
                        kernel_regularizer=regularization,
                        use_bias=False)(x)
    x = BatchNormalization()(x)
    x = Activation('relu')(x)
    x = SeparableConv2D(64, (3, 3), padding='same',
                        kernel_regularizer=regularization,
                        use_bias=False)(x)
    x = BatchNormalization()(x)

    x = MaxPooling2D((3, 3), strides=(2, 2), padding='same')(x)
    x = layers.add([x, residual])

    # module 4
    residual = Conv2D(128, (1, 1), strides=(2, 2),
                      padding='same', use_bias=False)(x)
    residual = BatchNormalization()(residual)

    x = SeparableConv2D(128, (3, 3), padding='same',
                        kernel_regularizer=regularization,
                        use_bias=False)(x)
    x = BatchNormalization()(x)
    x = Activation('relu')(x)
    x = SeparableConv2D(128, (3, 3), padding='same',
                        kernel_regularizer=regularization,
                        use_bias=False)(x)
    x = BatchNormalization()(x)

    x = MaxPooling2D((3, 3), strides=(2, 2), padding='same')(x)
    x = layers.add([x, residual])

    x = Conv2D(num_classes, (3, 3),
            #kernel_regularizer=regularization,
            padding='same')(x)
    x = GlobalAveragePooling2D()(x)
    output = Activation('softmax',name='predictions')(x)

    model = Model(img_input, output)
    return model

def train_model(X_train, Y_train, X_test, Y_test, X_validate, Y_validate, n_classes):
  
    # choose architecture
    model = my_cnn(X_train, n_classes)
    # model = arriaga_mini_XCEPTION((1, 48,48), n_classes)
    
    # compile model 
    model.compile(loss='categorical_crossentropy',
                  optimizer='adam',
                  metrics=['accuracy'])
    
    # Model Callbacks
    earlyStopping = EarlyStopping(monitor='val_loss', min_delta=0, patience=50, verbose=1, mode='auto')
 #   tensorBoard=TensorBoard(log_dir='./logs', histogram_freq=0, batch_size=32, write_graph=True, write_grads=False, write_images=True, embeddings_freq=0, embeddings_layer_names=None, embeddings_metadata=None)
    reduce_lr = ReduceLROnPlateau(monitor='val_loss', factor=0.1, cooldown=10,
                              patience=4, min_lr=0)
    model_checkpoint = ModelCheckpoint('saved_models/multiclass_models/best_weights.hd5', 'val_loss', verbose=1,
                                                    save_best_only=True)
    # Fit model on training data
    model.fit(X_train, Y_train, validation_data=(X_validate, Y_validate), callbacks=[model_checkpoint,reduce_lr,earlyStopping], 
              batch_size=32, epochs=1000, shuffle=True, verbose=2)

    # serialize model to JSON
    model_json = model.to_json()
    with open("saved_models/multiclass_models/model.json", "w") as json_file:
        json_file.write(model_json)

    # serialize weights to HDF5
    model.save_weights("saved_models/multiclass_models/model.h5", overwrite=True)
    
    print("Saved model to disk")
#    K.clear_session()
    return model

def model_metrics(model, X_test, Y_test):
    from sklearn.metrics import classification_report
    import numpy as np

    yt = np.argmax(Y_test, axis=1) # Convert one-hot to index
    y_pred = model.predict_classes(X_test)
    print(classification_report(yt, y_pred))

