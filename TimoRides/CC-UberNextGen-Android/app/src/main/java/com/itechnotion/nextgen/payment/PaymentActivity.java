package com.itechnotion.nextgen.payment;

import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;
import androidx.cardview.widget.CardView;

import com.itechnotion.nextgen.R;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class PaymentActivity extends AppCompatActivity {

    @BindView(R.id.ivBack)
    ImageView ivBack;
    @BindView(R.id.ll_wallet)
    LinearLayout llWallet;
    @BindView(R.id.ivproduct)
    ImageView ivproduct;
    @BindView(R.id.tvProductName)
    TextView tvProductName;
    @BindView(R.id.tvReviewcount)
    TextView tvReviewcount;
    @BindView(R.id.tvDone)
    TextView tvDone;
    @BindView(R.id.llTodays)
    CardView llTodays;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_payment);
        ButterKnife.bind(this);
    }

    @OnClick({R.id.ivBack, R.id.ll_wallet,R.id.tvDone})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ivBack:
                finish();
                break;
                case R.id.tvDone:
                finish();
                break;
            case R.id.ll_wallet:
                break;
        }
    }
}
