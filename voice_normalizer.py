# -*- coding: utf-8 -*-

# imports
from pydub import AudioSegment
from glob import glob
import numpy as np
import shutil
import os

# shutil copytree helper function (copy directories and subdirectories without files)
def ig_f(dir, files):
    return [f for f in files if os.path.isfile(os.path.join(dir, f))]

# normalize using target amplitude matching
def match_target_amplitude(sound, target_dBFS):

    change_in_dBFS = target_dBFS - sound.dBFS
    return sound.apply_gain(change_in_dBFS)

if __name__ == "__main__":

    ROOT_PATH_ACT1 = "Assets/Resources/audio/Act1"
    ROOT_PATH_ACT2 = "Assets/Resources/audio/BadCop"
    ROOT_PATH_ACT3 = "Assets/Resources/audio/Act3"

    filepaths_act1 = [y for x in os.walk(ROOT_PATH_ACT1) for y in glob(os.path.join(x[0], '*.wav'))]
    filepaths_act2 = [y for x in os.walk(ROOT_PATH_ACT2) for y in glob(os.path.join(x[0], '*.wav'))]
    filepaths_act3 = [y for x in os.walk(ROOT_PATH_ACT3) for y in glob(os.path.join(x[0], '*.wav'))]

    orig_wav_filepaths = filepaths_act1 + filepaths_act2 + filepaths_act3

    NEW_ROOT = "Assets/Resources/audio/normalized_voices"
    new_wav_filepaths = n_files = [NEW_ROOT + x.split('/audio')[1] for x in orig_wav_filepaths]

    # shutil.copytree('Assets/Resources/audio/Act1', 'Assets/Resources/audio/normalized_voices/Act1', ignore=ig_f)
    # shutil.copytree('Assets/Resources/audio/BadCop', 'Assets/Resources/audio/normalized_voices/BadCop', ignore=ig_f)
    # shutil.copytree('Assets/Resources/audio/Act3', 'Assets/Resources/audio/normalized_voices/Act3', ignore=ig_f)

    AVG_TARGET_AMPLITUDE = -15.0

    for idx, i in enumerate(orig_wav_filepaths):
        sound = AudioSegment.from_file(i, "wav")
        normalized_sound = match_target_amplitude(sound, AVG_TARGET_AMPLITUDE)
        normalized_sound.export(new_wav_filepaths[idx], format="wav")
