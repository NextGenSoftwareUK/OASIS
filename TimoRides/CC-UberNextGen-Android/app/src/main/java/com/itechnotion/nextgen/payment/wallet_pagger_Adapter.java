package com.itechnotion.nextgen.payment;

import android.content.Context;
import android.os.Parcelable;
import android.text.TextUtils;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.cardview.widget.CardView;
import androidx.viewpager.widget.PagerAdapter;

import com.bumptech.glide.Glide;
import com.itechnotion.nextgen.R;

import java.util.List;

public class wallet_pagger_Adapter extends PagerAdapter {


    private List<walletList> todayTradingBeanList;
    private LayoutInflater inflater;
    private Context context;
    OnTodaysProdSelectedListner onTodaysProdSelectedListner;
    CartCount cartCount;

    public wallet_pagger_Adapter(Context context, List<walletList> todayTradingBeans, OnTodaysProdSelectedListner onTodaysProdSelectedListner, CartCount cartCount) {
        this.context = context;
        this.todayTradingBeanList = todayTradingBeans;
        inflater = LayoutInflater.from(context);
        this.onTodaysProdSelectedListner = onTodaysProdSelectedListner;
        this.cartCount = cartCount;
    }

    @Override
    public void destroyItem(ViewGroup container, int position, Object object) {
        container.removeView((View) object);

    }

    public interface OnTodaysProdSelectedListner {
        void setOnTodaysProdSelectedListner(int position, walletList todayTradingBean);
    }

    @Override
    public int getCount() {
        return todayTradingBeanList.size();
    }

    @Override
    public Object instantiateItem(ViewGroup view, final int position) {


        View imageLayout = inflater.inflate(R.layout.wallet_pagger_layout, view, false);

        assert imageLayout != null;
        final ImageView ivproduct = (ImageView) imageLayout
                .findViewById(R.id.ivproduct);


        CardView llTodays = (CardView) imageLayout.findViewById(R.id.llTodays);
        TextView tvProductName = (TextView) imageLayout.findViewById(R.id.tvProductName);
        TextView tvReviewcount = (TextView) imageLayout.findViewById(R.id.tvReviewcount);
        TextView tvcash = (TextView) imageLayout.findViewById(R.id.tvcash);
        TextView tvTitle = (TextView) imageLayout.findViewById(R.id.tvTitle);

        Glide.with(context).load(todayTradingBeanList.get(position).getImg()).into(ivproduct);


        if (todayTradingBeanList.get(position).getName().equals("Cash")){
            tvTitle.setText("Balance");
            tvcash.setTextColor(context.getResources().getColor(R.color.colorPink));
        }else {
            tvTitle.setText("Card No");
            tvcash.setTextColor(context.getResources().getColor(R.color.colorfontBlack));

        }

        if (!TextUtils.isEmpty(todayTradingBeanList.get(position).getName())) {
            tvProductName.setText(todayTradingBeanList.get(position).getName());
        }

        tvcash.setText(todayTradingBeanList.get(position).getCount());

   //     Picasso.with(context).load(todayTradingBeanList.get(position).images.get(0).src).placeholder(R.drawable.placeholder).into(ivproduct);
/*
        if (databaseHelper.getProductInCart(id)) {
            Cart cart = databaseHelper.getProductFromCartById(id);
           // txtqunt.setText(cart.getQuantity() + "");
        } else {
           // txtqunt.setText("1");
        }*/



        llTodays.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                onTodaysProdSelectedListner.setOnTodaysProdSelectedListner(position, todayTradingBeanList.get(position));
            }
        });
        view.addView(imageLayout, 0);

        return imageLayout;
    }

    public interface CartCount {
        public void onCartListUpdate();

    }

    @Override
    public boolean isViewFromObject(View view, Object object) {
        return view.equals(object);
    }

    @Override
    public void restoreState(Parcelable state, ClassLoader loader) {
    }

    @Override
    public Parcelable saveState() {
        return null;
    }


}