package com.itechnotion.nextgen.setting;

import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;

import com.google.android.material.bottomsheet.BottomSheetDialog;
import com.itechnotion.nextgen.R;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class MyAccountActivity extends AppCompatActivity {

    @BindView(R.id.llProfile)
    LinearLayout llProfile;
    @BindView(R.id.ll_Birthday)
    LinearLayout llBirthday;
    @BindView(R.id.ll_phone)
    LinearLayout llPhone;
    @BindView(R.id.ivBack)
    ImageView ivBack;
    BottomSheetDialog mBottomSheetDialog;

    TextView ivCancel;


    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_my_account);
        ButterKnife.bind(this);
        mBottomSheetDialog = new BottomSheetDialog(this);
        View sheetView = getLayoutInflater().inflate(R.layout.birthday_bottom_sheet, null);
        mBottomSheetDialog.setContentView(sheetView);
        ivCancel=sheetView.findViewById(R.id.ivCancel);
        sheetView.setBackgroundResource(R.drawable.bg_bottom);
        //mBottomSheetDialog.show(getSupportFragmentManager(),mBottomSheetDialog.getTag());


        ivCancel.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mBottomSheetDialog.cancel();
            }
        });


    }

    @OnClick({R.id.ll_Birthday, R.id.ll_phone,R.id.ivBack})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ll_Birthday:
                mBottomSheetDialog.show();
                break;
            case R.id.ll_phone:
                break;
                case R.id.ivBack:
                    finish();
                break;

        }
    }


}
