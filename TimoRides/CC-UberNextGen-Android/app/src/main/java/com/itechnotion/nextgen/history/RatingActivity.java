package com.itechnotion.nextgen.history;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.RelativeLayout;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;

import com.itechnotion.nextgen.R;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class RatingActivity extends AppCompatActivity {

    @BindView(R.id.ll_wallet)
    RelativeLayout llWallet;
    @BindView(R.id.tvSubmitReview)
    TextView tvSubmitReview;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_rating);
        ButterKnife.bind(this);
    }

    @OnClick({R.id.ll_wallet, R.id.tvSubmitReview})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ll_wallet:
                finish();
                break;
            case R.id.tvSubmitReview:

                Intent intent=new Intent(RatingActivity.this,TipsActivity.class);
                startActivity(intent);
                break;
        }
    }
}
