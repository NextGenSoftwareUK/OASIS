package com.itechnotion.nextgen.setting;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.LinearLayout;

import androidx.appcompat.app.AppCompatActivity;

import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.loginsignup.LoginActivity;
import com.itechnotion.nextgen.notification.NotificationActivity;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class SettingActivity extends AppCompatActivity {

    @BindView(R.id.llProfile)
    LinearLayout llProfile;
    @BindView(R.id.lllogout)
    LinearLayout lllogout;
    @BindView(R.id.llNotify)
    LinearLayout llNotify;
  @BindView(R.id.ivBack)
  ImageView ivBack;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_setting);
        ButterKnife.bind(this);
    }

    @OnClick({R.id.llProfile, R.id.lllogout,R.id.ivBack,R.id.llNotify})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.llProfile:
                Intent  intent=new Intent(SettingActivity.this,MyAccountActivity.class);
                startActivity(intent);
                break;
            case R.id.lllogout:
                Intent intent1 = new Intent(SettingActivity.this, LoginActivity.class);
                intent1.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TASK | Intent.FLAG_ACTIVITY_NEW_TASK);
                startActivity(intent1);
                break;
                case R.id.llNotify:
                    Intent  intent2=new Intent(SettingActivity.this, NotificationActivity.class);
                    startActivity(intent2);
                break;
                case R.id.ivBack:
                    finish();
                break;
        }
    }
}
