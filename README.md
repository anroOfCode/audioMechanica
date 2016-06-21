# AudioMechanica

Nothing too exciting here yet. Some small experiments I've been working on to build up to algorithmic composition using RNNs and largely-unsupervised learning directly from waveform training data.

Likely to be a fool's errand.

Currently consists of:
* A iPython notebook prototyping out some FFT transforms and MEL visualizations.
* A C# project that captures the Windows audio loopback device and allows you to easily annotate music you're listening to with beat onsets to build up a respectable training set.

## Setup Instructions

* Currently using a Windows host
* Anaconda2 installed in C:\Anaconda2, 64-bit, Py2.7 ([source](https://www.continuum.io/downloads))
* VS2013 installed in default folder for complilation, VC folder added to path for pycuda operation.
* Git installed and added to path
* Followed instructions for CUDA7.5, mingw, libpython, theano, pycuda ([source](https://vanishingcodes.wordpress.com/2015/10/25/installing-cuda-7-5-and-pycuda-on-windows-for-testing-theano-with-gpu/))
* Jupyter notebooks started from Anaconda Command Prompt
* `pip install pyglet` for multimedia playback