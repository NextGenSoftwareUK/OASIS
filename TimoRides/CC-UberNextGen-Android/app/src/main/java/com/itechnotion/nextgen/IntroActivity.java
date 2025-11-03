package com.itechnotion.nextgen;

import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.os.Build;
import android.os.Bundle;
import android.text.Html;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowManager;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;
import androidx.viewpager.widget.PagerAdapter;
import androidx.viewpager.widget.ViewPager;

import com.itechnotion.nextgen.loginsignup.LoginActivity;
import com.itechnotion.nextgen.utils.PrefManager;

import butterknife.BindView;
import butterknife.ButterKnife;
// import me.huseyinozer.TooltipIndicator; // Commented out - old library removed


public class IntroActivity extends AppCompatActivity {
    @BindView(R.id.view_pager)
    ViewPager view_pager;
    @BindView(R.id.layoutDots)
    LinearLayout layoutDots;

    // Commented out - old library removed
    // @BindView(R.id.todays_dot_indicator)
    // TooltipIndicator todays_dot_indicator;
    //private ViewPager viewPager;
    private MyViewPagerAdapter myViewPagerAdapter;
  //  private LinearLayout dotsLayout;
    private TextView[] dots;
    private int[] layouts;
   // private ImageView ivprevious, ivNext;
    private PrefManager prefManager;
    private boolean isLastPageSwiped;
    private int counterPageScroll;
    boolean lastPageChange = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_intro);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        ButterKnife.bind(this);
        prefManager = new PrefManager(this);
        if (!prefManager.isFirstTimeLaunch()) {
            launchHomeScreen();
            finish();
        }

        // Making notification bar transparent
        if (Build.VERSION.SDK_INT >= 21) {
            getWindow().getDecorView().setSystemUiVisibility(View.SYSTEM_UI_FLAG_LAYOUT_STABLE | View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN);
        }

     /*   view_pager = (ViewPager) findViewById(R.id.view_pager);
        layoutDots = (LinearLayout) findViewById(R.id.layoutDots);*/
       /* ivprevious = (ImageView) findViewById(R.id.iv_previous);
        ivNext = (ImageView) findViewById(R.id.iv_next);*/


        // layouts of all welcome sliders
        // add few more layouts if you want
        layouts = new int[]{
                R.layout.welcome_slide1,
                R.layout.welcome_slide2,
                R.layout.welcome_slide3,
        };

        // adding bottom dots
        addBottomDots(0);

        // making notification bar transparent
        changeStatusBarColor();

        myViewPagerAdapter = new MyViewPagerAdapter();
        view_pager.setAdapter(myViewPagerAdapter);
        view_pager.addOnPageChangeListener(viewPagerPageChangeListener);
        // todays_dot_indicator.setupViewPager(view_pager); // Commented out - old library removed


    }
   /* @Override
    public void onClick(View view) {
        switch (view.getId()) {
            case R.id.iv_previous:


                break;
            case R.id.iv_next:

                break;

        }
    }*/
    private void addBottomDots(int currentPage) {
        dots = new TextView[layouts.length];

        int[] colorsActive = getResources().getIntArray(R.array.array_dot_active);
        int[] colorsInactive = getResources().getIntArray(R.array.array_dot_inactive);

        layoutDots.removeAllViews();
        for (int i = 0; i < dots.length; i++) {
            dots[i] = new TextView(this);
            dots[i].setText(Html.fromHtml("&#8227;"));
          //  dots[i].setText(Html.fromHtml("&#8226;"));
            dots[i].setTextSize(35);
            dots[i].setPadding(18,0,18,0);
            dots[i].setTextColor(colorsInactive[currentPage]);
            layoutDots.addView(dots[i]);
        }

        if (dots.length > 0)
            dots[currentPage].setTextColor(colorsActive[currentPage]);
    }

    private int getItem(int i) {
        return view_pager.getCurrentItem() + i;
    }

    private void launchHomeScreen() {
        prefManager.setFirstTimeLaunch(false);
        startActivity(new Intent(IntroActivity.this, LoginActivity.class));
        finish();
    }

    //  viewpager change listener
    ViewPager.OnPageChangeListener viewPagerPageChangeListener = new ViewPager.OnPageChangeListener() {

        @Override
        public void onPageSelected(int position) {
            addBottomDots(position);

            // changing the next button text 'NEXT' / 'GOT IT'
            if (position == layouts.length - 3) {

            } else {
                // still pages are left
                //iv_previous.setVisibility(View.VISIBLE);
            }

        }

        @Override
        public void onPageScrolled(int arg0, float arg1, int arg2) {

            Log.e(String.valueOf(arg0),"first");
            Log.e(String.valueOf(arg1),"second");
            Log.e(String.valueOf(arg2),"third");

          /*  if (position == 6 && positionOffset == 0 && !isLastPageSwiped){
                if(counterPageScroll != 0){
                    isLastPageSwiped=true;
                    //Next Activity here
                }
                counterPageScroll++;
            }else{
                counterPageScroll=0;
            }
*/



        }

        @Override
        public void onPageScrollStateChanged(int arg0) {

            Log.e(String.valueOf(arg0),"selected");
            int lastIdx = myViewPagerAdapter.getCount() - 1;

            int curItem = view_pager.getCurrentItem();
            if(curItem==lastIdx  && arg0==1){
                lastPageChange = true;
                prefManager.setFirstTimeLaunch(false);
                startActivity(new Intent(IntroActivity.this, LoginActivity.class));
                finish();

                // i put this here since onPageScroll gets called a couple of times.
              //  finish();
            }else  {
                lastPageChange = false;
            }
        }
    };

    /**
     * Making notification bar transparent
     */
    private void changeStatusBarColor() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            Window window = getWindow();
            window.addFlags(WindowManager.LayoutParams.FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);
            window.setStatusBarColor(Color.TRANSPARENT);
        }
    }

    /**
     * View pager adapter
     */
    public class MyViewPagerAdapter extends PagerAdapter {
        private LayoutInflater layoutInflater;

        public MyViewPagerAdapter() {
        }

        @Override
        public Object instantiateItem(ViewGroup container, int position) {
            layoutInflater = (LayoutInflater) getSystemService(Context.LAYOUT_INFLATER_SERVICE);

            View view = layoutInflater.inflate(layouts[position], container, false);
            container.addView(view);

            return view;
        }

        @Override
        public int getCount() {
            return layouts.length;
        }

        @Override
        public boolean isViewFromObject(View view, Object obj) {
            return view == obj;
        }


        @Override
        public void destroyItem(ViewGroup container, int position, Object object) {
            View view = (View) object;
            container.removeView(view);
        }
    }
}