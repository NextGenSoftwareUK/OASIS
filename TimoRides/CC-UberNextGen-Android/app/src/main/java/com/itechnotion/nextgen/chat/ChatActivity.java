package com.itechnotion.nextgen.chat;

import android.os.Bundle;
import android.text.TextUtils;
import android.view.View;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.LinearLayout;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.itechnotion.nextgen.R;

import java.util.ArrayList;
import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;

public class ChatActivity extends AppCompatActivity {

    @BindView(R.id.ivBack)
    ImageView ivBack;
    @BindView(R.id.llmain)
    LinearLayout llmain;
    @BindView(R.id.chat_recycler_view)
    RecyclerView chatRecyclerView;
    @BindView(R.id.chat_input_msg)
    EditText chatInputMsg;
    @BindView(R.id.chat_send_msg)
    ImageView chatSendMsg;
    @BindView(R.id.lltxt)
    LinearLayout lltxt;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_chat);
        ButterKnife.bind(this);

        // Get RecyclerView object.
        final RecyclerView msgRecyclerView = (RecyclerView) findViewById(R.id.chat_recycler_view);

        // Set RecyclerView layout manager.
        LinearLayoutManager linearLayoutManager = new LinearLayoutManager(this);
        msgRecyclerView.setLayoutManager(linearLayoutManager);

        // Create the initial data list.
        final List<ChatAppMsgDTO> msgDtoList = new ArrayList<ChatAppMsgDTO>();
        //  todayTradingList.add(new walletList("1", "Cash", "fgfg", "df"));
        msgDtoList.add(new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_SENT, "Hello, are you nearby?", false, "Today at 4:00 PM"));
        msgDtoList.add(new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_RECEIVED, "I'll be there in a few mins", true, "null"));
        msgDtoList.add(new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_SENT, "OK,I am waiting at \nMansi Cross Roads", false, "null"));
        msgDtoList.add(new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_RECEIVED, "Sorry,I'm stuck in traffic, \nPlease give me a moment.", true, "4:03 PM"));
      /*  ChatAppMsgDTO msgDtoseder = new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_SENT, "");
        ChatAppMsgDTO msgDto1 = new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_RECEIVED, "hello");
        ChatAppMsgDTO msgDtoseder1 = new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_SENT, "how r u?");*/
        ;


        // Create the data adapter with above data list.
        final ChatAppMsgAdapter chatAppMsgAdapter = new ChatAppMsgAdapter(msgDtoList);

        // Set data adapter to RecyclerView.
        msgRecyclerView.setAdapter(chatAppMsgAdapter);

        final EditText msgInputText = (EditText) findViewById(R.id.chat_input_msg);

        ImageView msgSendButton = (ImageView) findViewById(R.id.chat_send_msg);

        msgSendButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                String msgContent = msgInputText.getText().toString();

                if (!TextUtils.isEmpty(msgContent)) {
                    // Add a new sent message to the list.
                    ChatAppMsgDTO msgDto = null;
                    for (int i = 0; i < msgDtoList.size(); i++) {

                        if (msgDtoList.get(i).isValue()) {
                            msgDto = new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_SENT, msgContent, false, "null");


                        } else {
                            msgDto = new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_RECEIVED, msgContent, true, "null");
                            // msgDtoList.add(msgDto);
                        }
                    }
                    //  ChatAppMsgDTO msgDto1 = new ChatAppMsgDTO(ChatAppMsgDTO.MSG_TYPE_RECEIVED, msgContent);
                    //  msgDtoList.add(msgDto1);
                    msgDtoList.add(msgDto);
                    int newMsgPosition = msgDtoList.size() - 1;

                    // Notify recycler view insert one new data.
                    chatAppMsgAdapter.notifyItemInserted(newMsgPosition);

                    // Scroll RecyclerView to the last message.
                    msgRecyclerView.scrollToPosition(newMsgPosition);

                    // Empty the input edit text box.
                    msgInputText.setText("");
                }
            }
        });

    }

    @OnClick({R.id.ivBack, R.id.llmain})
    public void onViewClicked(View view) {
        switch (view.getId()) {
            case R.id.ivBack:
                finish();
                break;
            case R.id.llmain:
                break;
        }
    }
}
