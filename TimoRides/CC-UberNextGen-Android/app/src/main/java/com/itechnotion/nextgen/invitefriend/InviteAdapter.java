package com.itechnotion.nextgen.invitefriend;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.payment.walletList;

import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;

public class InviteAdapter extends RecyclerView.Adapter<InviteAdapter.MyViewHolder> {
    List<walletList> searchList;
    Context context;

    public InviteAdapter(List<walletList> searchList, Context context) {
        this.searchList = searchList;
        this.context = context;
    }

    @Override
    public MyViewHolder onCreateViewHolder(ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.invite_layout, parent, false);
        MyViewHolder holder = new MyViewHolder(v);
        return holder;
    }

    @Override
    public void onBindViewHolder(MyViewHolder holder, final int position) {
        holder.tvProductName.setText(searchList.get(position).getName());
       // holder.tvReviewcount.setText(searchList.get(position).getSrc());

        if (searchList.get(position).getImgVal().equals("1")){
            holder.ivproduct.setImageResource(R.drawable.ic_added);
        }else if (searchList.get(position).getImgVal().equals("2")){
            holder.ivproduct.setImageResource(R.drawable.ic_add);
        }

        holder.itemView.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
              /*  Intent appInfo = new Intent(context, InviteCodeActivity.class);
                context.startActivity(appInfo);*/

            }
        });
    }

    @Override
    public int getItemCount() {
        /*return categorylist.size();*/
        return searchList.size();
    }

    public class MyViewHolder extends RecyclerView.ViewHolder {
        @BindView(R.id.tvProductName)
        TextView tvProductName;


        @BindView(R.id.ivproduct)
        ImageView ivproduct;


        public MyViewHolder(View itemView) {
            super(itemView);
            ButterKnife.bind(this, itemView);
        }
    }

}
