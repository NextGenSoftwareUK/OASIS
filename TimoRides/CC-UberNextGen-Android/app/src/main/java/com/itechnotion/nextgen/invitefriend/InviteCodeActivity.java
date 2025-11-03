package com.itechnotion.nextgen.invitefriend;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.RelativeLayout;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;

import com.itechnotion.nextgen.R;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class InviteCodeActivity extends AppCompatActivity {

    @BindView(R.id.ivBack)
    ImageView ivBack;
    @BindView(R.id.ll_wallet)
    RelativeLayout llWallet;
    @BindView(R.id.tvTitle)
    ImageView tvTitle;
    @BindView(R.id.lllogo)
    LinearLayout lllogo;
    @BindView(R.id.tvTerms)
    TextView tvTerms;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_invite_code);
        ButterKnife.bind(this);
    }

    @OnClick({R.id.ivBack, R.id.tvTerms})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ivBack:
                finish();
                break;
            case R.id.tvTerms:
                Intent intent=new Intent(InviteCodeActivity.this,InviteFrndActivity.class);
                startActivity(intent);
                break;
        }
    }
}
