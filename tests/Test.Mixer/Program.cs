using System.Runtime.InteropServices;
using DNA.Mixer;
using Pie.SDL;

unsafe
{
    const uint sampleRate = 44100;
    
    AudioMixer mixer = new AudioMixer(sampleRate, 10);

    byte[] audioData = File.ReadAllBytes(@"C:\Users\ollie\Music\TESTFILES\plokboss-32bitfloat.raw");
    AudioBuffer buffer = mixer.CreateBuffer<byte>(new BufferInfo(BufferType.PCM, new AudioFormat(DataType.F32, 48000, 2)), audioData);
    mixer.PlayBuffer(buffer, 0);

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
        mixer.MixStereoFloat(new Span<float>(arg1, arg2 / 4));
    }

    while (true)
    {
        Thread.Sleep(1000);
    }
    
    Sdl.CloseAudioDevice(device);
    Sdl.QuitSubSystem(Sdl.InitAudio);
    
    handle.Free();
}