# Facade
_Facade_ is a game we develop to explore a new way of achieving high level of immersion: emotion detection. By reading player's facial expression during the game, the in-game characters will use the facial expression reading to communicate better with the player.

## Facial Expression Recognition (FER)
The requirements to use FER in this game are:
* Laptop camera (duh)
* Python 3.x
* Anaconda
* A bunch of libraries but let's get to that later

Follow these steps to activate FER:

### Step 1: Install Anaconda
https://www.anaconda.com/download/ FYOS (Find Your Own OS)

### Step 2: Create the Environment (is that what you call it?)
Use the following command:

```conda create -n env1 python=3.6 opencv tensorflow keras matplotlib```

This will create the environment and name it `env1`. It will also install the libraries written after `python=3.6`. If you wanna look into these libraries go ahead but basically this command does the installation for you.

### Step 3: Activate the Environment
```conda activate env1```

Congrats you're inside the environment now

### Step 4: Install One More Library
Just one more so bear with me okay

```pip install args```

### Step 5: Test if FER is Working
I think we need a testing code just to check everything works but we don't have it yet so let's just name it `test_code.py` for now

```python test_code.py```

If you see [whatever the intended output is] then FER is working!

![wow](https://pics.me.me/such-problem-much-fix-very-tech-such-support-wow-2737205.png)

### Step 6: Run the FER
Go to the `StreamingAssets/FERModel` folder, then run the following command:

```python video_emotion_color_demo.py```

If you see a bunch of print statements about TensorFlow library, you're on the right track. If you see the message saying `Compiled loaded model`, then FER is ready!

### Step 7: Deactivate the Environment
This should be obvious but do this only when you're done playing around with FER.

```conda deactivate```


