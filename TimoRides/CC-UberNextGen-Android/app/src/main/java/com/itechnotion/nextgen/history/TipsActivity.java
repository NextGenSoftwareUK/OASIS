package com.itechnotion.nextgen.history;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.RelativeLayout;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;

import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.home.HomepageActivity;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class TipsActivity extends AppCompatActivity {

    @BindView(R.id.ll_wallet)
    RelativeLayout llWallet;
    @BindView(R.id.tvSubmitReview)
    TextView tvSubmitReview;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_tips);
        ButterKnife.bind(this);
    }

    @OnClick({R.id.ll_wallet, R.id.tvSubmitReview})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ll_wallet:
                finish();
                break;
            case R.id.tvSubmitReview:
                Intent intent = new Intent(TipsActivity.this, HomepageActivity.class);
                intent.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TASK | Intent.FLAG_ACTIVITY_NEW_TASK);
                startActivity(intent);
                break;
        }
    }
}
