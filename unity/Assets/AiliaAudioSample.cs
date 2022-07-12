/* ailia.audio sample program */
/* Copyright 2022 AXELL CORPORATION */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AiliaAudioSample : MonoBehaviour
{
    public AudioClip clip;  // Inference target
    
    // Parameters
    const int MELS=128;
    const bool DEBUG_MODE=false;

    // Start is called before the first frame update
    void Start()
    {
        // Get pcm from audio clip
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);   //float range

        // Get mel spectrum using ailia.audio
        float [] mel_spectrum=MelSpectrum(samples,clip.samples,clip.channels,clip.frequency);
        if(mel_spectrum==null){
            Debug.Log("MelSpectrum failed");
            return;
        }
        
        // Predict audio tag
        Prediction(mel_spectrum);
    }

    // Get tags from file
    void LoadTags(List<string> name,string from){
        using (var reader = new StreamReader(from)){
            while(true){
                string data = reader.ReadLine();
                if(data==null){
                    return;
                }
                name.Add(data);
            }
        }
    }

    // Normalize model output
    void PostProcessing(float [] dst){
        for(int i=0;i<dst.Length;i++){
            dst[i]=Mathf.Exp(dst[i]);
        }
    }

    // Predict audio tag from mel spectrum
    void Prediction(float [] melspectrogram){
        int num_samples=melspectrogram.Length / MELS;

        // Load labels
        List<string> name=new List<string>();
        LoadTags(name,Application.streamingAssetsPath+"/name.txt");

        // Calculate batch mean and std
        float mean=0;
        float mean2=0;
        float std_val=0;
        
        for(int i=0;i<num_samples*MELS;i++){
            mean=mean+melspectrogram[i];
            mean2=mean2+melspectrogram[i]*melspectrogram[i];
        }
        mean=mean/(num_samples*MELS);
        mean2=mean2/(num_samples*MELS);
        std_val=Mathf.Sqrt(mean2 - mean*mean);

        // Normalize input melspectrum
        float[] normalized_melspectrogram=new float[melspectrogram.Length];
        for(int i=0;i<num_samples*MELS;i++){
            normalized_melspectrogram[i]=(melspectrogram[i]-mean)/std_val;
        }

        // Open model file
        AiliaModel ailia=new AiliaModel();
        ailia.OpenFile(null,Application.streamingAssetsPath+"/crnn_audio_classification.onnx");

        // Set input
        Ailia.AILIAShape shape=new Ailia.AILIAShape();
        shape.x=(uint)(melspectrogram.Length / MELS);
        shape.y=MELS;
        shape.z=1;
        shape.w=1;
        shape.dim=4;
        ailia.SetInputBlobShape(shape,ailia.FindBlobIndexByName("data"));
        ailia.SetInputBlobData(normalized_melspectrogram,ailia.FindBlobIndexByName("data"));
        float [] len=new float[1];
        len[0]=melspectrogram.Length / MELS;
        ailia.SetInputBlobData(len,ailia.FindBlobIndexByName("lengths"));

        // Infer
        ailia.Update();

        // Get output
        Ailia.AILIAShape output_shape=ailia.GetOutputShape();
        float [] dst=new float[output_shape.x];
        ailia.GetBlobData(dst,ailia.FindBlobIndexByName("conf"));
        ailia.Close();

        // Post processing
        PostProcessing(dst);

        // Get best result
        int max_id=0;
        for(int i=0;i<dst.Length;i++){
            if(DEBUG_MODE){
                Debug.Log(""+name[i]+" "+dst[i]);
            }
            if(dst[i]>=dst[max_id]){
                max_id = i;
            }
        }
        Debug.Log(name[max_id]+"/"+dst[max_id]);
    }

    // Get melspectrum using ailia.audio
    float [] MelSpectrum(float [] samples,int clip_samples,int clip_channels,int clip_frequency){
        // Convert stereo to mono
        float[] data = new float[clip_samples];
        for (int i = 0; i < data.Length; ++i)
        {
            if(clip_channels==1){
                data[i]=samples[i];
            }else{
                data[i]=(samples[i*2+0]+samples[i*2+1])/2;
            }
        }

        // Get melspectrum
        int len = data.Length;
        const int SAMPLE_RATE=44100;
        const int FFT_N=2048;
        const int HOP_N=1024;
        const int WIN_N=2048;
        const float POWER=2;
        const float F_MIN=0.0f;
        float f_max=44100/2;

        int frame_len=0;
        int status = AiliaAudio.ailiaAudioGetFrameLen(ref frame_len, len, FFT_N, HOP_N, AiliaAudio.AILIA_AUDIO_STFT_CENTER_ENABLE);
        if(status!=0){
            Debug.Log("ailiaAudioGetFrameLen failed %d\n"+status);
            return null;
        }
        if(DEBUG_MODE){
            Debug.Log("samplingRate:"+clip_frequency);
            Debug.Log("samples:"+clip_samples);
            Debug.Log("frame_len:"+frame_len);
        }

        float [] melspectrogram = new float [MELS*frame_len];
        status=AiliaAudio.ailiaAudioGetMelSpectrogram(melspectrogram, data, len, SAMPLE_RATE, FFT_N, HOP_N, WIN_N, AiliaAudio.AILIA_AUDIO_WIN_TYPE_HANN, 
            frame_len, AiliaAudio.AILIA_AUDIO_STFT_CENTER_ENABLE, POWER, AiliaAudio.AILIA_AUDIO_FFT_NORMALIZE_NONE ,F_MIN, f_max, MELS, 
            AiliaAudio.AILIA_AUDIO_MEL_NORMALIZE_NONE,AiliaAudio.AILIA_AUDIO_MEL_SCALE_FORMULA_HTK);
        if(status!=0){
            Debug.Log("ailiaAudioGetMelSpectrogram failed "+status);
            return null;
        }

        return melspectrogram;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
