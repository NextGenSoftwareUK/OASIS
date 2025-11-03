package com.itechnotion.nextgen;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;

/**
 * Created by cd on 24-10-2017.
 */

public class Adapter extends RecyclerView.Adapter<Adapter.MyViewHolder> {
    List<Bean> categorylist;
    Context context;

    public Adapter(List<Bean> categorylist, Context context) {
        this.categorylist = categorylist;
        this.context = context;
    }

    @Override
    public MyViewHolder onCreateViewHolder(ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_specification, parent, false);
        MyViewHolder holder = new MyViewHolder(v);
        return holder;
    }

    @Override
    public void onBindViewHolder(MyViewHolder holder, final int position) {


        holder.tv_name.setText(categorylist.get(position).getName());

        holder.tv_value.setText(categorylist.get(position).getDate());


      //  Picasso.with(context).load(categorylist.get(position).getThumbnail_id()).resize(300, 300).into(holder.ivcat);

    }

    @Override
    public int getItemCount() {
        /*return categorylist.size();*/
        return categorylist.size();
    }

    public class MyViewHolder extends RecyclerView.ViewHolder {
        @BindView(R.id.tv_name)
        TextView tv_name;

        @BindView(R.id.tv_value)
        TextView tv_value;


        public MyViewHolder(View itemView) {
            super(itemView);
            ButterKnife.bind(this, itemView);
        }
    }

}
