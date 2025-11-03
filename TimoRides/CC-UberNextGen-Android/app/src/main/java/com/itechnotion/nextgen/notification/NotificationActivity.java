package com.itechnotion.nextgen.notification;

import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.LinearLayout;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.RecyclerView;

import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.payment.walletList;

import java.util.ArrayList;
import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class NotificationActivity extends AppCompatActivity {

    @BindView(R.id.ll_wallet)
    LinearLayout llWallet;
    @BindView(R.id.ivBack)
    ImageView ivBack;
    @BindView(R.id.rvNotification)
    RecyclerView rvNotification;
    private ArrayList<Integer> todaysImagesArray = new ArrayList<Integer>();
    private List<walletList> todayTradingList = new ArrayList<>();
    NotifyAdapter notifyAdapter;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_notification);
        ButterKnife.bind(this);

        getValueForNotification();
    }
    private void getValueForNotification() {

        todayTradingList.add(new walletList( "System", "Your booking #1234 hase been success", "1"));
        todayTradingList.add(new walletList( "System", "Your booking #1234 hase been success", "1"));
        todayTradingList.add(new walletList( "System", "Invite friends - Get 2 coupons each!", "3"));
        todayTradingList.add(new walletList( "System", "Invite friends - Get 2 coupons each!", "3"));
        todayTradingList.add(new walletList( "System", "Your booking #1234 hase been Cancel", "2"));
        todayTradingList.add(new walletList( "System", "Your booking #1234 hase been Cancel", "2"));



      //  rvNotification.setAdapter(new NotifyAdapter( todayTradingList, this));
        notifyAdapter = new NotifyAdapter(todayTradingList, this);
        rvNotification.setAdapter(notifyAdapter);
        notifyAdapter.notifyDataSetChanged();
        // Auto start of viewpager

    }
    @OnClick({R.id.ll_wallet,R.id.ivBack})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ll_wallet:
                break;
                case R.id.ivBack:
                    finish();
                break;

        }
    }
}
