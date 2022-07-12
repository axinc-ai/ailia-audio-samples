/* ailia Audio Unity Plugin Native Interface */
/* Copyright 2021 AXELL CORPORATION */

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class AiliaAudio
{

    /****************************************************************
    * パラメータ定義
    **/

    public const Int32 AILIA_AUDIO_WIN_TYPE_HANN                 =( 1);    /* 窓関数に hann 窓を使う */
    public const Int32 AILIA_AUDIO_WIN_TYPE_HAMMING              =( 2);    /* 窓関数に hamming 窓を使う */

    public const Int32 AILIA_AUDIO_STFT_CENTER_NONE              =( 0);    /* STFT の際、前後に padding を入れない */
    public const Int32 AILIA_AUDIO_STFT_CENTER_ENABLE            =( 1);    /* STFT の際、sample_n の前後に hop_n/2 の padding を入れる */

    public const Int32 AILIA_AUDIO_FFT_NORMALIZE_NONE            =( 0);    /* FFT の出力を正規化しない */
    public const Int32 AILIA_AUDIO_FFT_NORMALIZE_LIBROSA_COMPAT  =( 1);    /* FFT の出力を librosa 互換で正規化する */
    public const Int32 AILIA_AUDIO_FFT_NORMALIZE_PYTORCH_COMPAT  =( 1);    /* FFT の出力を PyTorch 互換で正規化する */
    public const Int32 AILIA_AUDIO_FFT_NORMALIZE_SCIPY_COMPAT    =( 2);    /* FFT の出力を SciPy 互換で正規化する */

    public const Int32 AILIA_AUDIO_MEL_NORMALIZE_NONE            =( 0);    /* MEL スペクトログラムの出力を正規化しない */
    public const Int32 AILIA_AUDIO_MEL_NORMALIZE_ENABLE          =( 1);    /* MEL スペクトログラムの出力を正規化する */

    public const Int32 AILIA_AUDIO_MEL_SCALE_FORMULA_HTK         =( 1);    /* MEL 尺度を HTK formula で求める (PyTorch 互換) */
    public const Int32 AILIA_AUDIO_MEL_SCALE_FORMULA_SLANYE      =( 0);    /* MEL 尺度を Slanye's formula で求める (librosa デフォルト互換) */

    public const Int32 AILIA_AUDIO_PHASE_FORM_COMPLEX            =( 1);    /* 位相を複素数形式で出力する (librosa デフォルト互換) */
    public const Int32 AILIA_AUDIO_PHASE_FORM_REAL               =( 0);    /* 位相を実数形式で出力する (PyTorch デフォルト互換) */

    /* Native Binary 定義 */

    #if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_WEBGL && !UNITY_EDITOR)
        public const String LIBRARY_NAME="__Internal";
    #else
        #if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
            public const String LIBRARY_NAME="ailia_audio";
        #else
            public const String LIBRARY_NAME="ailia_audio";
        #endif
    #endif

    /*************************************************************
    * 入力値を対数スケールに変換します。
    *   引数:
    *     dst - 出力データポインタ、float 型、長さ src_n
    *     src - 入力データポインタ、float 型、長さ src_n
    *     src_n - 計算対象の要素数
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *     dst = log_e(1.0 + src) を計算します。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioLog1p(float[] dst, float[] src, int src_n);

    /*************************************************************
    * 非負の入力値をデシベルスケールに変換します。
    *   引数:
    *     dst - 出力データポインタ、float 型、長さ src_n
    *     src - 入力データポインタ、float 型、要素数 src_n
    *     src_n - 計算対象の要素数
    *     top_db - 出力の最大値から出力下限の閾値までを定める値 (>= 0.0)、負数の場合は処理は閾値を設定しない
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *     librosa.power_to_dbと互換性があります。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioConvertPowerToDB(float[] dst, float[] src, int src_n, float top_db);


    /*************************************************************
    * STFTで生成される時間フレーム長を取得します。
    *   引数:
    *     frame_n - フレーム長出力先ポインタ
    *     sample_n - STFTを適用するサンプル数
    *     fft_n - FFT点数
    *     hop_n - 窓のシフト数
    *     center - AILIA_AUDIO_STFT_CENTER_* のいずれか
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   備考:
    *     STFT実行前のバッファサイズの決定に使用します。
    *     AILIA_AUDIO_STFT_CENTER_NONE の場合 hop_n ずつ区切り、sample_n の前後に padding を行いません。
    *     AILIA_AUDIO_STFT_CENTER_ENABLE の場合 sample_n の前後に hop_n/2 の padding を行います。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetFrameLen(ref Int32 frame_n, int sample_n, int fft_n, int hop_n, int center);


    /*************************************************************
    * 窓関数の係数を取得します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 window_n
    *     window_n - 窓の長さ（サンプル数）
    *     win_type - 窓関数の種類、AILIA_AUDIO_WIN_TYPE_* のいずれか
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   備考:
    *     窓関数はhann窓とhamming窓のみ対応しています。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetWindow(float[] dst, int window_n, int win_type);


    /*************************************************************
    * FFTを実行します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から fft_n, 2(実部、虚部) 順のメモリレイアウト
    *     src - 入力データのポインタ、float 型、要素数 fft_n
    *     fft_n - FFT点数（2の累乗）
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   備考:
    *     FFT点数が2の累乗の場合のみ対応しています。
    *     出力データは実部と虚部の交互信号であり、長さは fft_n*2 です。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioFFT(float[] dst, float[] src, int fft_n);


    /*************************************************************
    * 音響信号からスペクトログラムを生成します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から frame_n, freq_n, 2(複素数: 実部, 虚部) 順のメモリレイアウト
    *     src - 入力データのポインタ、float 型、要素数 sample_n
    *     sample_n - 入力データのサンプル数
    *     fft_n - FFT点数（2の累乗）
    *     hop_n - フレームのシフト数
    *     win_type: 窓関数の種類、AILIA_AUDIO_WIN_TYPE_* のいずれか
    *     max_frame_n - 出力データの時間フレーム数の最大値
    *     center -  入力データの前後へのパディングの有無、AILIA_AUDIO_STFT_CENTER_* のいずれか
    *     power - スペクトログラムの乗数（>= 0.0） 0.0: 複素スペクトログラム、1.0: 振幅スペクトログラム、2.0: パワースペクトログラム、その他: 任意の累乗値の出力に相当
    *     norm_type - FFT後の正規化処理、AILIA_AUDIO_FFT_NORMALIZE_* のいずれか
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *     時間フレームごとにFFT→正規化処理→累乗（振幅・パワーに変換）の順で処理を実行します。
    *   備考:
    *     FFT点数が2の累乗の場合のみ、窓関数の長さはFFT点数と等しい場合のみに対応しています。
    *     出力データは実部と虚部の交互信号であり、長さは(fft_n/2+1)*時間フレーム長*2です。
    *     powerが0.0以外の場合は虚部の値を全て0.0として出力します。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetSpectrogram(float[] dst, float[] src, int sample_n, int fft_n, int hop_n, int win_n, int win_type, int max_frame_n, int center, float power, int norm_type);


    /*************************************************************
    * メルフィルタバンクの係数を計算します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から freq_n, mel_n 順のメモリレイアウト
    *     freq_n - 周波数のインデックス数
    *     f_min - 周波数の最小値
    *     f_max - 周波数の最大値
    *     mel_n - メル周波数のインデックス数（ < freq_n）
    *     sample_rate - サンプリング周波数
    *     mel_norm - 出力される係数の正規化の有無、AILIA_AUDIO_MEL_NORMALIZE_* のいずれか
    *     mel_formula - MEL尺度の形式、AILIA_AUDIO_MEL_SCALE_FORMULA_* のいずれか
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   備考:
    *     FFT点数が2の累乗の場合のみ対応しています。
    *     出力データは実部と虚部の交互信号であり、長さは(fft_n/2+1)*2です。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetFBMatrix(float[] dst, int freq_n,  float f_min, float f_max, int mel_n, int sample_rate, int mel_norm, int mel_formula);


    /*************************************************************
    * 音響信号からメルスペクトログラムを生成します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、外側から frame_n, mel_n 順のメモリレイアウト
    *     src - 入力データのポインタ、float 型、モノラル PCM データ
    *     sample_n - 入力データのサンプル数
    *     sample_rate - サンプリング周波数
    *     fft_n - FFT点数（2の累乗）
    *     hop_n - フレームのシフト数
    *     win_n - 1フレームに含むサンプル数（ < fft_n）
    *     win_n < fft_n の場合、フレーム末尾にゼロパディング
    *     win_type: 窓関数の種類、AILIA_AUDIO_WIN_TYPE_* のいずれか
    *     max_frame_n - 出力データの時間フレーム数の最大値
    *     center -  入力データの前後へのパディングの有無、AILIA_AUDIO_STFT_CENTER_* のいずれか
    *     power - スペクトログラムの乗数（ > 0.0）1.0: 振幅スペクトログラム、2.0: パワースペクトログラム、その他: 任意の累乗値の出力に相当
    *     fft_norm_type - FFT後の正規化処理、AILIA_AUDIO_FFT_NORMALIZE_* のいずれか
    *     f_min - 周波数の最小値
    *     f_max - 周波数の最大値
    *     mel_n - メル周波数のインデックス数（ < freq_n）
    *     mel_norm_type - MELスペクトログラムの正規化の有無、AILIA_AUDIO_MEL_NORMALIZE_* のいずれか
    *     mel_formula - MEL尺度の形式、AILIA_AUDIO_MEL_SCALE_FORMULA_* のいずれか
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *     時間フレームごとにFFT(STFT)→正規化処理→累乗（振幅・パワーに変換→メルフィルタバンクの係数取得→メル尺度への変換 の順で処理を実行します。
    *   備考:
    *     FFT点数が2の累乗の場合のみ、窓関数の長さはFFT点数と等しい場合のみに対応しています。
    *     出力データは実数の信号であり、長さはmel_n*時間フレーム長です。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioGetMelSpectrogram(float[] dst, float[] src, int sample_n, int sample_rate, int fft_n, int hop_n, int win_n, int win_type, int max_frame_n, int center, float power, int fft_norm_type, float f_min, float f_max, int mel_n, int mel_norm_type, int mel_formula);


    /*************************************************************
    * スペクトログラムから振幅と位相を計算します。
    *   引数:
    *     dst_mag - 振幅の出力先ポインタ、外側から frame_n, freq_n 順のメモリレイアウト
    *     dst_phase - 位相の出力先ポインタ、外側から frame_n, freq_n, 2(実部、虚部) 順のメモリレイアウト
    *     src - 入力データのポインタ、frame_n, freq_n, 2(実部、虚部) 順のメモリレイアウト
    *     freq_n - 周波数のインデックス数
    *     frame_n - 時間フレームの数
    *     power - 振幅スペクトルの乗数 ( > 0.0)、1.0: 振幅スペクトログラム、2.0: パワースペクトログラムに相当
    *     phase_form - 位相の出力形式、AILIA_AUDIO_PHASE_FORM_* のいずれか
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   備考:
    *     librosaのデフォルト値と互換の条件: phase_form = AILIA_AUDIO_PHASE_FORM_COMPLEX, power = 1.0
    *     PyTorchのデフォルト値と互換の条件: phase_form = AILIA_AUDIO_PHASE_FORM_REAL, power = 1.0
    *     phase_formによってdst_phaseの出力が変わります。
    *       - AILIA_AUDIO_PHASE_FORM_COMPLEX : 実部と虚部の交互信号、サイズは freq_n * frame_n * 2
    *       - AILIA_AUDIO_PHASE_FORM_REAL : 実部のみの信号、サイズは freq_n * frame_n
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioMagPhase(float[] dst_mag, float[] dst_phase, float[] src, int freq_n, int frame_n, float power, int phase_form);


    /*************************************************************
    * 実数の信号に対して標準化を実行します。
    *   引数:
    *     dst - 出力データのポインタ、float 型、要素数 src_n
    *     src - 入力データのポインタ、float 型、要素数 src_n
    *     src_n - 入力データの要素数
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *     入力データの平均0、分散1になるよう標準化を行う。
    *     dst = (src - mean(src)) / std(src)
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioStandardize(float[] dst, float[] src, int src_n);


    /*************************************************************
    * 実数スペクトログラム/メルスペクトログラムの時間フレーム数を調整します。
    *   引数:
    *     dst - 出力データのポインタ、dst_frame_n, freq_n 順のメモリレイアウト
    *     src - 入力データのポインタ、src_frame_n, freq_n 順のメモリレイアウト
    *     freq_n - 周波数のインデックス数
    *     dst_frame_n - 出力データの時間フレームの数
    *     src_frame_n  - 入力データの時間フレームの数
    *     pad_data - パディング（dst_frame_n > src_frame_n の場合に使用）
    *   返値:
    *     成功した場合はAILIA_STATUS_SUCCESS、そうでなければエラーコードを返す。
    *   解説:
    *     dst_frame_n > src_frame_n : 不足する時間フレームのデータを pad_data のデータで埋める。
    *     dst_frame_n <= src_frame_n : 先頭から dst_frame_n のデータのみを切り出す。
    **/
    [DllImport(LIBRARY_NAME)]
    public static extern int ailiaAudioFixFrameLen(float[] dst, float[] src, int freq_n, int dst_frame_n, int src_frame_n, float pad_data);
}
