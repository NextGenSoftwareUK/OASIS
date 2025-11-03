package com.itechnotion.nextgen.loginsignup;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.view.WindowManager;
import android.widget.EditText;
import android.widget.LinearLayout;

import androidx.appcompat.app.AppCompatActivity;

import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.home.HomepageActivity;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class OTPActivity extends AppCompatActivity {

    @BindView(R.id.tvone)
    EditText tvone;
    @BindView(R.id.vone)
    View vone;
    @BindView(R.id.llone)
    LinearLayout llone;
    @BindView(R.id.tvtwo)
    EditText tvtwo;
    @BindView(R.id.vtwo)
    View vtwo;
    @BindView(R.id.lltwo)
    LinearLayout lltwo;
    @BindView(R.id.tvthree)
    EditText tvthree;
    @BindView(R.id.vthree)
    View vthree;
    @BindView(R.id.llthree)
    LinearLayout llthree;
    @BindView(R.id.tvfour)
    EditText tvfour;
    @BindView(R.id.vfour)
    View vfour;
    @BindView(R.id.llfour)
    LinearLayout llfour;
    @BindView(R.id.llVerify)
    LinearLayout llVerify;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);

        setContentView(R.layout.activity_otp);
        ButterKnife.bind(this);
    }

    @OnClick(R.id.llVerify)
    public void onViewClicked() {
        Intent intent=new Intent(OTPActivity.this, HomepageActivity.class);
        startActivity(intent);

    }
}
