# imports
import matplotlib
import sys
import args
from train_model_emotion import train_model, model_metrics
from load_model_emotion import eval_model, load_model
from preprocess_inputs import process_inputs, load_save_inputs
from predict_emotion import predict, select
from pprint import pprint
from sklearn.metrics import confusion_matrix
import matplotlib.pyplot as plt
import brewer2mpl
import numpy as np
import pandas as pd
set3 = brewer2mpl.get_map('Set3', 'qualitative', 6).mpl_colors

def return_inputs(bool_val):
    
    print("\nRETURN INPUTS FUNCTION")
    (X_train, Y_train), (X_test, Y_test), (X_validate, Y_validate) = load_save_inputs(bool_val, saved_inputs='saved_inputs', filename="../../fer2013/fer2013.csv")
    return X_train, Y_train, X_test, Y_test, X_validate, Y_validate

def create_model(X_train, Y_train, X_test, Y_test, X_validate, Y_validate, n_classes):
    
    print("\nCREATE MODEL FUNCTION")
    model = train_model(X_train, Y_train, X_test, Y_test, X_validate, Y_validate, n_classes)
    model_metrics(model, X_test, Y_test)
    
    return model

def get_model(path_inputs="saved_models/multiclass_models/model.json", path_weights="saved_models/multiclass_models/model.h5"):
    
    print("\nLOAD N GET MODEL FUNCTION")
    model = load_model(path_inputs, path_weights)
    
    return model

def evaluate_model(model, X_train, Y_train, X_test, Y_test, X_validate, Y_validate):
    
    print("\nEVALUATE MODEL FUNCTION")
    return eval_model(model, X_test, Y_test)

def predict_class(model, path='happy', label=3):
    
    print("\nPREDICT CLASS FUNTION FOR: " + str(select(path)))
    pprint (predict(select(path), label, model))

labels = ['angry', 'fear', 'happy', 'sad', 'surprise', 'neutral']

def plot_confusion_matrix(y_true, y_pred, cmap, labels):
    
    cm = confusion_matrix(y_true, y_pred)
    print(cm)
    fig = plt.figure(figsize=(6,6))
    matplotlib.rcParams.update({'font.size': 16})
    ax  = fig.add_subplot(111)
    matrix = ax.imshow(cm, interpolation='nearest', cmap=cmap)
    fig.colorbar(matrix)
    for i in range(0,6):
        for j in range(0,6):
            ax.text(j,i,cm[i,j],va='center', ha='center')
    # ax.set_title('Confusion Matrix')
    ticks = np.arange(len(labels))
    ax.set_xticks(ticks)
    ax.set_xticklabels(labels, rotation=45)
    ax.set_yticks(ticks)
    ax.set_yticklabels(labels)
    plt.tight_layout()
    plt.ylabel('True label')
    plt.xlabel('Predicted label')

if __name__=='__main__':

    bool_val=False
    X_train, Y_train, X_test, Y_test, X_validate, Y_validate = return_inputs(bool_val)

    compare=[]
    for i in range(1):
    	# model = create_model(X_train, Y_train, X_test, Y_test, X_validate, Y_validate, 6)
        loadmodel = get_model(path_inputs="saved_models/multiclass_models/model.json", path_weights="saved_models/multiclass_models/best_weights.hd5")
        eval = evaluate_model(loadmodel, X_train, Y_train, X_test, Y_test, X_validate, Y_validate)
        y_pred = loadmodel.predict_classes(X_test)
        y_true = np.argmax(Y_test, axis=1)
        #labels = ['neutral', 'anger', 'fear', 'happy', 'sad', 'surprise']
        # plot_confusion_matrix(y_true, y_pred, plt.cm.YlGnBu, labels)
        predict_class(loadmodel, path='happy', label=3)
        #compare.append(eval)
