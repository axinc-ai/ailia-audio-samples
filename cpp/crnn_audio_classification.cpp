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

void dump_tensor(const char *id,const float *data){
#ifdef DEBUG_DUMP
	printf("%s\n",id);
	for(int i=0;i<128;i++){
		if(i<16 || i>=128-16){
			for(int j=0;j<128;j++){
				if(j<16 || j>=128-16){
					printf("%f ",data[i*128+j]);
				}
			}
			printf("\n");
		}
	}
#endif
}

void postprocess(std::vector<float> &dst){
	for(int i=0;i<dst.size();i++){
		dst[i]=exp(dst[i]);
	}
}

int predict(AILIANetwork *ailia,std::vector<float> &src){
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

	printf("input shape %d %d %d %d %d\n",input_shape.x,input_shape.y,input_shape.z,input_shape.w,input_shape.dim);

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

	printf("output shape %d %d %d %d %d\n",output_shape.x,output_shape.y,output_shape.z,output_shape.w,output_shape.dim);

	std::vector<float> dst(output_shape.x*output_shape.y*output_shape.z*output_shape.w);

	printf("output shape %d %d %d %d %d\n", output_shape.x, output_shape.y, output_shape.z, output_shape.w,
		   output_shape.dim);

	status = ailiaGetBlobData(ailia, &dst[0], dst.size() * sizeof(float), output_idx);
	if (status != AILIA_STATUS_SUCCESS) {
		printf("ailiaGetBlobData Failed %d", status);
		return -1;
	}

	postprocess(dst);

	//Dsiplay probablity
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

int load_file(std::vector<short> &buf,int *sampleRate,int *nChannels,int *nSamples,const char *from){
	WaveReader reader;
	reader.open_a(from);
	buf.resize(reader.get_total_sample_n());
	reader.get_wave(&buf[0],0,reader.get_total_sample_n(),1);
	*sampleRate = reader.get_sampling_rate();
	*nChannels = reader.get_channel_n();
	*nSamples = reader.get_total_sample_n() / reader.get_channel_n();
	printf("sampleRate : %d nChannels : %d nSamples : %d\n",*sampleRate,*nChannels,*nSamples);
	return 0;
}

int inference(std::vector<float> &melspectrogram){
	//Dsiplay probablity
	dump_tensor("melspectgram",&melspectrogram[0]);

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

	dump_tensor("input",&normalized_melspectrogram[0]);

	AILIANetwork *ailia;

	int env_id=AILIA_ENVIRONMENT_ID_AUTO;

	printf("SELECTED ENVIRONMENT ID:%d\n",env_id);

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

	predict(ailia,normalized_melspectrogram);

	ailiaDestroy(ailia);

	return 0;
}

int main(int argc, char **argv){
	//const char *from="./24965__www-bonson-ca__bigdogbarking-02.wav";
	const char *from="./dog.wav";
	if(argc!=2){
		//printf("Usage  : cnn_audio_claffification input.wav\n");
		//return -1;
	}else{
		from=argv[1];
	}


	std::vector<short> buf;
	int sampleRate,nChannels,nSamples;
	int status=load_file(buf,&sampleRate,&nChannels,&nSamples,from);
	if(status!=0){
		return -1;
	}

	int len = nSamples;
	std::vector<float> data(len);

	for(int i=0;i<len;i++){
		if(nChannels==2){
			data[i]=(buf[i*2+0]+buf[i*2+1])/2/32768.0f;
		}else{
			data[i]=buf[i]/32768.0f;
		}
	}

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

	printf("\n");
	printf("frame len %d\n",frame_len);

	std::vector<float> melspectrogram(MELS*frame_len);

	status=ailiaAudioGetMelSpectrogram(&melspectrogram[0], &data[0], len, SAMPLE_RATE, FFT_N, HOP_N, WIN_N, AILIA_AUDIO_WIN_TYPE_HANN, 
		frame_len, AILIA_AUDIO_STFT_CENTER_ENABLE, POWER, AILIA_AUDIO_FFT_NORMALIZE_NONE ,F_MIN, f_max, MELS, 
		AILIA_AUDIO_MEL_NORMALIZE_NONE,AILIA_AUDIO_MEL_SCALE_FORMULA_HTK);
	if(status!=0){
		printf("ailiaAudioGetMelSpectrogram failed %d\n",status);
		return -1;
	}

	inference(melspectrogram);
}


