package com.itechnotion.nextgen.history;

import android.content.Context;
import android.content.Intent;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.payment.walletList;
import com.itechnotion.nextgen.ride.BookingRequestActivity;

import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;

public class HistoryAdapter extends RecyclerView.Adapter<HistoryAdapter.MyViewHolder> {
    List<walletList> searchList;
    Context context;

    public HistoryAdapter(List<walletList> searchList, Context context) {
        this.searchList = searchList;
        this.context = context;
    }

    @Override
    public MyViewHolder onCreateViewHolder(ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.history_layout, parent, false);
        MyViewHolder holder = new MyViewHolder(v);
        return holder;
    }

    @Override
    public void onBindViewHolder(MyViewHolder holder, final int position) {
        holder.tvStatus.setText(searchList.get(position).getImgVal());

        holder.tvPrise.setText(searchList.get(position).getName());

        if (searchList.get(position).getImgVal().equals("confirm")){
            holder.tvStatus.setTextColor(context.getResources().getColor(R.color.colorBlue));
        }else if (searchList.get(position).getImgVal().equals("completed")){
            holder.tvStatus.setTextColor(context.getResources().getColor(R.color.colorgreen));
        }else if (searchList.get(position).getImgVal().equals("cancelled")){
          //  holder.ivproduct.setImageResource(R.drawable.ic_add);
            holder.tvStatus.setTextColor(context.getResources().getColor(R.color.colorGrey));

        }

        holder.itemView.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if (searchList.get(position).getImgVal().equals("confirm")){
                    Intent appInfo = new Intent(context, BookingRequestActivity.class);
                    context.startActivity(appInfo);
                }else if (searchList.get(position).getImgVal().equals("completed")){
                    Intent appInfo = new Intent(context, RatingActivity.class);
                    context.startActivity(appInfo);
                }else if (searchList.get(position).getImgVal().equals("cancelled")){
                    //  holder.ivproduct.setImageResource(R.drawable.ic_add);
                  //  holder.tvStatus.setTextColor(context.getResources().getColor(R.color.colorGrey));

                }


            }
        });
    }

    @Override
    public int getItemCount() {
        /*return categorylist.size();*/
        return searchList.size();
    }

    public class MyViewHolder extends RecyclerView.ViewHolder {
        @BindView(R.id.tvStatus)
        TextView tvStatus;
 @BindView(R.id.tvPrise)
        TextView tvPrise;





        public MyViewHolder(View itemView) {
            super(itemView);
            ButterKnife.bind(this, itemView);
        }
    }

}
