package com.itechnotion.nextgen.utils;

import android.content.Context;
import androidx.appcompat.app.AppCompatDelegate;
import androidx.multidex.MultiDexApplication;

public class MyApplication extends MultiDexApplication {
    
    private Context mContext;
    private static MyApplication instance;

    @Override
    public void onCreate() {
        super.onCreate();
        instance = this;
        mContext = this;
        AppCompatDelegate.setCompatVectorFromResourcesEnabled(true);
    }

    public static MyApplication getInstance() {
        return instance;
    }
    
    public Context getContext() {
        return mContext;
    }
}
