package com.itechnotion.nextgen.invitefriend;

import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.RelativeLayout;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.RecyclerView;

import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.payment.walletList;

import java.util.ArrayList;
import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class InviteFrndActivity extends AppCompatActivity {

    @BindView(R.id.ll_wallet)
    RelativeLayout llWallet;
    @BindView(R.id.ivBack)
    ImageView ivBack;
    @BindView(R.id.rvInvite)
    RecyclerView rvInvite;
    private ArrayList<Integer> todaysImagesArray = new ArrayList<Integer>();
    private List<walletList> list = new ArrayList<>();
    InviteAdapter inviteAdapter;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_invite_frnd);
        ButterKnife.bind(this);
        getValueForInvite();
    }
    private void getValueForInvite() {

        list.add(new walletList( "John Malik", "John Malik", "1"));
        list.add(new walletList( "John Malik", "John Malik", "2"));
        list.add(new walletList( "John Malik", "John Malik", "1"));
        list.add(new walletList( "John Malik", "John Malik", "1"));
        list.add(new walletList( "John Malik", "John Malik", "2"));
        list.add(new walletList( "John Malik", "John Malik", "2"));



        //  rvNotification.setAdapter(new NotifyAdapter( todayTradingList, this));
        inviteAdapter = new InviteAdapter(list, this);
        rvInvite.setAdapter(inviteAdapter);
        inviteAdapter.notifyDataSetChanged();
        // Auto start of viewpager

    }
    @OnClick({R.id.ll_wallet, R.id.rvInvite,R.id.ivBack})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ll_wallet:
                break;
            case R.id.rvInvite:
                break;
                case R.id.ivBack:
                    finish();
                break;
        }
    }
}
