package com.itechnotion.nextgen.history;

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

public class HistoryActivity extends AppCompatActivity {

    @BindView(R.id.ll_wallet)
    LinearLayout llWallet;
    @BindView(R.id.ivBack)
    ImageView ivBack;
    @BindView(R.id.rvHistory)
    RecyclerView rvHistory;
    private List<walletList> list = new ArrayList<>();
    HistoryAdapter historyAdapter;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_history);
        ButterKnife.bind(this);
        getValueForHistory();
    }
    private void getValueForHistory() {

        list.add(new walletList( "$75.00", "John Malik", "confirm"));
        list.add(new walletList( "$30.00", "John Malik", "completed"));
        list.add(new walletList( "$45.00", "John Malik", "cancelled"));
        list.add(new walletList( "$55.00", "John Malik", "cancelled"));

        //  rvNotification.setAdapter(new NotifyAdapter( todayTradingList, this));
        historyAdapter = new HistoryAdapter(list, this);
        rvHistory.setAdapter(historyAdapter);
        historyAdapter.notifyDataSetChanged();
        // Auto start of viewpager

    }
    @OnClick({R.id.ll_wallet, R.id.ivBack})
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
