﻿using System.Runtime.InteropServices;
using DNA.Mixer;
using Pie.SDL;

unsafe
{
    const uint sampleRate = 48000;
    
    AudioMixer mixer = new AudioMixer(sampleRate, 10);
    //mixer.InterpolationMode = InterpolationMode.None;

    byte[] audioData = File.ReadAllBytes(@"C:\Users\ollie\Music\TESTFILES\greengrove-16bitshort.raw");
    AudioBuffer buffer = mixer.CreateBuffer<byte>(new BufferInfo(BufferType.PCM, new AudioFormat(DataType.I16, 44100, 2)), audioData);
    mixer.PlayBuffer(buffer, 0, new VoiceProperties()
    {
        Pitch = 1.0
    });

    if (Sdl.Init(Sdl.InitAudio) < 0)
        throw new Exception("SDL did not init: " + Sdl.GetErrorS());

    SdlAudioSpec spec;
    spec.Freq = (int) sampleRate;
    spec.Format = Sdl.AudioF32;
    spec.Channels = 2;
    spec.Samples = 512;

    Sdl.AudioCallback callback = new Sdl.AudioCallback(AudioCallback);
    GCHandle handle = GCHandle.Alloc(callback);

    spec.Callback = (delegate*<void*, byte*, int, void>) Marshal.GetFunctionPointerForDelegate(callback);
    
    uint device = Sdl.OpenAudioDevice(null, 0, &spec, null, 0);
    
    Sdl.PauseAudioDevice(device, 0);

    void AudioCallback(void* arg0, byte* arg1, int arg2)
    {
        mixer.MixFloat(new Span<float>(arg1, arg2 / 4), SoundSetup.Stereo);
    }

    while (true)
    {
        Thread.Sleep(1000);

        //mixer.GetVoicePropertiesRef(0).Speed += 0.1;
    }
    
    Sdl.CloseAudioDevice(device);
    Sdl.QuitSubSystem(Sdl.InitAudio);
    
    handle.Free();
}