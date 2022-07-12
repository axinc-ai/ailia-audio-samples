export SDK_PATH=./ailia_1_211_0
cp ${SDK_PATH}/library/mac/libailia_audio.dylib ./unity/Assets/Plugins/macOS/libailia_audio.bundle
cp ${SDK_PATH}/library/mac/libailia.dylib ./unity/Assets/Plugins/macOS/libailia.bundle
cp ${SDK_PATH}/library/windows/x64/ailia_audio.dll ./unity/Assets/Plugins/x86_64/
cp ${SDK_PATH}/library/windows/x64/ailia.dll ./unity/Assets/Plugins/x86_64/
cp ${SDK_PATH}/library/android/armeabi-v7a/libailia_audio.so ./unity/Assets/Plugins/android/armeabi-v7a/
cp ${SDK_PATH}/library/android/armeabi-v7a/libailia.so ./unity/Assets/Plugins/android/armeabi-v7a/
cp ${SDK_PATH}/library/android/arm64-v8a/libailia_audio.so ./unity/Assets/Plugins/android/arm64-v8a/
cp ${SDK_PATH}/library/android/arm64-v8a/libailia.so ./unity/Assets/Plugins/android/arm64-v8a/
cp ${SDK_PATH}/library/ios/libailia_audio.a ./unity/Assets/Plugins/ios/
cp ${SDK_PATH}/library/ios/libailia.a ./unity/Assets/Plugins/ios/
cp ${SDK_PATH}/library/linux/libailia_audio.so ./unity/Assets/Plugins/linux/
cp ${SDK_PATH}/library/linux/libailia.so ./unity/Assets/Plugins/linux/