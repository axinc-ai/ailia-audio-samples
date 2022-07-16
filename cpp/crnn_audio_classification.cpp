/*******************************************************************
*
*    DESCRIPTION:
*      CRNN Audio Claffication
*    AUTHOR:
*      ax Inc.
*    DATE:2022/07/14
*
*******************************************************************/

#include <stdio.h>
#include <math.h>
#include <vector>
#include <string>

#include "ailia.h"
#include "ailia_audio.h"

#include "wave_reader.h"

const int MELS = 128;

static const char *classes[10] = {\
        "air_conditioner", "car_horn", "children_playing", "dog_bark",\
        "drilling", "engine_idling", "gun_shot", "jackhammer", "siren",\
        "street_music"\
};

void postprocess(std::vector<float> &dst){
	for(int i=0;i<dst.size();i++){
		dst[i]=exp(dst[i]);
	}
}

int infer(AILIANetwork *ailia,std::vector<float> &src){
	int status;

	unsigned int input_idx = 0;
	status = ailiaFindBlobIndexByName(ailia, &input_idx, "data");
	if (status != AILIA_STATUS_SUCCESS) {
		printf("ailiaFindBlobIndexByName Failed %d", status);
		return -1;
	}

	unsigned int length_idx = 0;
	status = ailiaFindBlobIndexByName(ailia, &length_idx, "lengths");
	if (status != AILIA_STATUS_SUCCESS) {
		printf("ailiaFindBlobIndexByName Failed %d", status);
		return -1;
	}

	unsigned int output_idx = 0;
	status = ailiaFindBlobIndexByName(ailia, &output_idx, "conf");
	if (status != AILIA_STATUS_SUCCESS) {
		printf("ailiaFindBlobIndexByName Failed %d", status);
		return -1;
	}

	AILIAShape input_shape;
	input_shape.x=src.size()/MELS;
	input_shape.y=MELS;
	input_shape.z=1;
	input_shape.w=1;
	input_shape.dim=4;
	status=ailiaSetInputBlobShape(ailia,&input_shape,input_idx,AILIA_SHAPE_VERSION);
	if(status!=AILIA_STATUS_SUCCESS){
		printf("ailiaSetInputShape Failed %d",status);
		return -1;
	}

	printf("input shape %d %d %d %d dims %d\n",input_shape.x,input_shape.y,input_shape.z,input_shape.w,input_shape.dim);

	if(src.size()!=input_shape.x*input_shape.y*input_shape.z*input_shape.w){
		printf("input shape error\n");
		return -1;
	}

	status = ailiaSetInputBlobData(ailia, &src[0], src.size() * sizeof(float), input_idx);
	if (status != AILIA_STATUS_SUCCESS) {
		printf("ailiaSetInputBlobData Failed %d", status);
		return -1;
	}

	float length = src.size() / MELS;
	status = ailiaSetInputBlobData(ailia, &length, sizeof(float), length_idx);
	if (status != AILIA_STATUS_SUCCESS) {
		printf("ailiaSetInputBlobData Failed %d", status);
		return -1;
	}

	status = ailiaUpdate(ailia);
	if (status != AILIA_STATUS_SUCCESS) {
		printf("ailiaUpdate Failed %d", status);
		return -1;
	}

	AILIAShape output_shape;
	status=ailiaGetBlobShape(ailia,&output_shape,output_idx,AILIA_SHAPE_VERSION);
	if(status!=AILIA_STATUS_SUCCESS){
		printf("ailiaGetOutputShape Failed %d",status);
		return -1;
	}

	printf("output shape %d %d %d %d dims %d\n",output_shape.x,output_shape.y,output_shape.z,output_shape.w,output_shape.dim);

	std::vector<float> dst(output_shape.x*output_shape.y*output_shape.z*output_shape.w);

	status = ailiaGetBlobData(ailia, &dst[0], dst.size() * sizeof(float), output_idx);
	if (status != AILIA_STATUS_SUCCESS) {
		printf("ailiaGetBlobData Failed %d", status);
		return -1;
	}

	postprocess(dst);

	printf("predict result\n");
	int max_i=0;
	for(int i=0;i<dst.size();i++){
		if(dst[max_i] < dst[i]){
			max_i = i;
		}
	}
	printf("%s %f\n",classes[max_i],dst[max_i]);

	return 0;
}

int inference(std::vector<float> &melspectrogram){
	// preprocess
	float mean = 0.0f;
	float mean2 = 0.0f;
	float std_val = 1.0f;
	for(int i=0;i<melspectrogram.size();i++){
		mean = mean + melspectrogram[i];
		mean2 = mean2 + melspectrogram[i]*melspectrogram[i];
	}
    mean=mean/melspectrogram.size();
    mean2=mean2/melspectrogram.size();
    std_val=sqrt(mean2 - mean*mean);

	std::vector<float> normalized_melspectrogram(melspectrogram.size());
	for(int i=0;i<melspectrogram.size();i++){
		normalized_melspectrogram[i]=(melspectrogram[i]-mean)/std_val;
	}

	// open onnx
	AILIANetwork *ailia;
	int env_id=AILIA_ENVIRONMENT_ID_AUTO;

	int status=ailiaCreate(&ailia,env_id,AILIA_MULTITHREAD_AUTO);
	if(status!=AILIA_STATUS_SUCCESS){
		printf("ailiaCreate Failed %d",status);
		return -1;
	}

	status=ailiaOpenWeightFile(ailia,"crnn_audio_classification.onnx");
	if(status!=AILIA_STATUS_SUCCESS){
		printf("ailiaOpenWeightFile Failed %d",status);
		return -1;
	}

	// infer
	infer(ailia,normalized_melspectrogram);

	ailiaDestroy(ailia);

	return 0;
}

int main(int argc, char **argv){
	const char *from="./24965__www-bonson-ca__bigdogbarking-02.wav";
	//const char *from="./dog.wav";
	if(argc!=2){
		//printf("Usage  : cnn_audio_claffification input.wav\n");
		//return -1;
	}else{
		from=argv[1];
	}

	// Load wave file
	int sampleRate,nChannels,nSamples;
	std::vector<float> buf = read_wave_file(from,&sampleRate,&nChannels,&nSamples);
	int status;

	if(buf.size()==0){
		printf("wav file not found or could not open %s\n",from);
		return -1;
	}

	// Convert to mono
	int len = nSamples;
	std::vector<float> data(len);
	for(int i=0;i<len;i++){
		if(nChannels==2){
			data[i]=(buf[i*2+0]+buf[i*2+1])/2;
		}else{
			data[i]=buf[i];
		}
	}

	// Convert to Melspectrum
	const int SAMPLE_RATE=44100;
	const int FFT_N=2048;
	const int HOP_N=1024;
	const int WIN_N=2048;
	const float POWER=2;
	const float F_MIN=0.0;
	float f_max=SAMPLE_RATE/2;

	int frame_len;
	status = ailiaAudioGetFrameLen(&frame_len, len, FFT_N, HOP_N, AILIA_AUDIO_STFT_CENTER_ENABLE);
	if(status!=0){
		printf("ailiaAudioGetFrameLen failed %d\n",status);
		return -1;
	}

	std::vector<float> melspectrogram(MELS*frame_len);

	status=ailiaAudioGetMelSpectrogram(&melspectrogram[0], &data[0], len, SAMPLE_RATE, FFT_N, HOP_N, WIN_N, AILIA_AUDIO_WIN_TYPE_HANN, 
		frame_len, AILIA_AUDIO_STFT_CENTER_ENABLE, POWER, AILIA_AUDIO_FFT_NORMALIZE_NONE ,F_MIN, f_max, MELS, 
		AILIA_AUDIO_MEL_NORMALIZE_NONE,AILIA_AUDIO_MEL_SCALE_FORMULA_HTK);
	if(status!=0){
		printf("ailiaAudioGetMelSpectrogram failed %d\n",status);
		return -1;
	}

	// Infer
	inference(melspectrogram);

	return 0;
}


