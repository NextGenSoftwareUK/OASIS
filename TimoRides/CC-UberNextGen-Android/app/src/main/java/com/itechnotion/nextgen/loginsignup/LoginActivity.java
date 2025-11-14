package com.itechnotion.nextgen.loginsignup;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;

import com.itechnotion.nextgen.home.HomepageActivity;
import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.utils.BaseActivity;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;



public class LoginActivity extends BaseActivity {

    @BindView(R.id.vSignup)
    View vSignup;
    @BindView(R.id.llSignup)
    LinearLayout llSignup;
    @BindView(R.id.vSignin)
    View vSignin;
    @BindView(R.id.llSignin)
    LinearLayout llSignin;
    @BindView(R.id.edtPhone)
    EditText edtPhone;
    @BindView(R.id.btnNext)
    Button btnNext;
    @BindView(R.id.progress_signin)
    ProgressBar progressSignin;
    @BindView(R.id.btnFb)
    Button btnFb;
    @BindView(R.id.progress_fb)
    ProgressBar progressFb;
    @BindView(R.id.llMain)
    LinearLayout llMain;
    @BindView(R.id.tvSignup)
    TextView tvSignup;
    @BindView(R.id.tvTerms)
    TextView tvTerms;
    @BindView(R.id.tvSignin)
    TextView tvSignin;
    @BindView(R.id.layoutSignin)
    LinearLayout layoutSignin;
    @BindView(R.id.edtEmail)
    EditText edtEmail;
    @BindView(R.id.edtPassword)
    EditText edtPassword;
    @BindView(R.id.layoutSignup)
    LinearLayout layoutSignup;
    @BindView(R.id.llInBtn)
    LinearLayout llInBtn;
    @BindView(R.id.btnSignup)
    Button btnSignup;
    @BindView(R.id.progress_signup)
    ProgressBar progressSignup;
    @BindView(R.id.llUpbtn)
    LinearLayout llUpbtn;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        setContentView(R.layout.activity_login);
        ButterKnife.bind(this);
       // tvSignup.setTextColor(getColor(R.color.colorBlack));
        tvSignup.setTextColor(getResources().getColor(R.color.colorBlack));
        tvSignin.setTextColor(getResources().getColor(R.color.colorGrey));
        tvSignin.setBackgroundTintList(getResources().getColorStateList(R.color.colorWhite));
        vSignup.setBackgroundTintList(getResources().getColorStateList(R.color.colorGrey));
        layoutSignup.setVisibility(View.VISIBLE);
        layoutSignin.setVisibility(View.GONE);
        llInBtn.setVisibility(View.GONE);
        llUpbtn.setVisibility(View.VISIBLE);
        tvTerms.setVisibility(View.VISIBLE);
    }

    @OnClick({R.id.llSignup, R.id.llSignin, R.id.btnNext, R.id.btnFb,R.id.btnSignup})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.llSignup:

                tvSignup.setTextColor(getResources().getColor(R.color.colorBlack));
                tvSignin.setTextColor(getResources().getColor(R.color.colorGrey));
                vSignin.setBackgroundTintList(getResources().getColorStateList(R.color.colorWhite));
                vSignup.setBackgroundTintList(getResources().getColorStateList(R.color.colorGrey));
                layoutSignup.setVisibility(View.VISIBLE);
                layoutSignin.setVisibility(View.GONE);
                llInBtn.setVisibility(View.GONE);
                llUpbtn.setVisibility(View.VISIBLE);
                tvTerms.setVisibility(View.VISIBLE);


                break;
            case R.id.llSignin:
                tvSignin.setTextColor(getResources().getColor(R.color.colorBlack));
                tvSignup.setTextColor(getResources().getColor(R.color.colorGrey));
                vSignin.setBackgroundTintList(getResources().getColorStateList(R.color.colorGrey));
                vSignup.setBackgroundTintList(getResources().getColorStateList(R.color.colorWhite));
                layoutSignup.setVisibility(View.GONE);
                layoutSignin.setVisibility(View.VISIBLE);
                llInBtn.setVisibility(View.VISIBLE);
                llUpbtn.setVisibility(View.GONE);
                tvTerms.setVisibility(View.GONE);

                break;
            case R.id.btnNext:
                Intent intent=new Intent(LoginActivity.this, OTPActivity.class);
                startActivity(intent);
                break;
                case R.id.btnSignup:
                    tvSignin.setTextColor(getResources().getColor(R.color.colorBlack));
                    tvSignup.setTextColor(getResources().getColor(R.color.colorGrey));
                    vSignin.setBackgroundTintList(getResources().getColorStateList(R.color.colorGrey));
                    vSignup.setBackgroundTintList(getResources().getColorStateList(R.color.colorWhite));
                    layoutSignup.setVisibility(View.GONE);
                    layoutSignin.setVisibility(View.VISIBLE);
                    llInBtn.setVisibility(View.VISIBLE);
                    llUpbtn.setVisibility(View.GONE);
                    tvTerms.setVisibility(View.GONE);
                break;

            case R.id.btnFb:
                Intent intent1=new Intent(LoginActivity.this, HomepageActivity.class);
                startActivity(intent1);
                break;
        }
    }
}
