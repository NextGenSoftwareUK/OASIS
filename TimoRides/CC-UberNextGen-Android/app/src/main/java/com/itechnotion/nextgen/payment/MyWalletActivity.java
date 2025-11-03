package com.itechnotion.nextgen.payment;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.RelativeLayout;

import androidx.appcompat.app.AppCompatActivity;
import androidx.viewpager.widget.ViewPager;

import com.itechnotion.nextgen.R;
// import com.viewpagerindicator.CirclePageIndicator; // Commented out - library removed

import java.util.ArrayList;
import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class MyWalletActivity extends AppCompatActivity implements wallet_pagger_Adapter.OnTodaysProdSelectedListner, wallet_pagger_Adapter.CartCount {

    @BindView(R.id.ll_wallet)
    LinearLayout llWallet;
    @BindView(R.id.ivBack)
    ImageView ivBack;
    @BindView(R.id.image_view_pager)
    ViewPager imageViewPager;
    // @BindView(R.id.dot_indicator)
    // CirclePageIndicator dotIndicator; // Commented out - library removed
    private static int todayscurrentPage = 0;
    private static int todaysNUM_PAGES = 0;
    private static final Integer[] todaysIMAGES = {R.drawable.ic_cash, R.drawable.ic_cash, R.drawable.ic_cash};
    @BindView(R.id.rlViewpager)
    RelativeLayout rlViewpager;
    @BindView(R.id.llPayment)
    LinearLayout llPayment;
    private ArrayList<Integer> todaysImagesArray = new ArrayList<Integer>();
    private List<walletList> todayTradingList = new ArrayList<>();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_my_wallet);
        ButterKnife.bind(this);
        getValueForTodaysTreding();
    }

    private void getValueForTodaysTreding() {

        todayTradingList.add(new walletList("1", "Cash", R.drawable.ic_cash, "$2500"));
        todayTradingList.add(new walletList("1", "Credit Card", R.drawable.ic_credit_card, "**** **** **** 1234"));
        todayTradingList.add(new walletList("1", "Visa", R.drawable.ic_visa_logo, "**** **** **** 4567"));
        imageViewPager.setClipToPadding(false);
        imageViewPager.setPadding(0, 0, 0, 0);
        imageViewPager.setPageMargin(25);
        for (int i = 0; i < todaysIMAGES.length; i++)
            todaysImagesArray.add(todaysIMAGES[i]);
        imageViewPager.setAdapter(new wallet_pagger_Adapter(this, todayTradingList, this, this));
        // dotIndicator.setViewPager(imageViewPager); // Commented out - library removed
        todaysNUM_PAGES = todaysIMAGES.length;
        imageViewPager.setCurrentItem(todayscurrentPage++, true);
        // Auto start of viewpager

    }

    @Override
    public void setOnTodaysProdSelectedListner(int position, walletList todayTradingBean) {

    }

    @Override
    public void onCartListUpdate() {

    }

    @OnClick({R.id.ll_wallet, R.id.llPayment,R.id.ivBack})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ll_wallet:
                break;
                case R.id.ivBack:
                    finish();
                break;
            case R.id.llPayment:
                Intent intent=new Intent(MyWalletActivity.this,PaymentActivity.class);
                startActivity(intent);
                break;
        }
    }
}
