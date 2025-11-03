package com.itechnotion.nextgen.chat;

import java.util.ArrayList;

/**
 * Created by cd on 24-10-2017.
 */

public class chatBean {

    private String id;
    private String name;
    private String src;
    private String count;
    private String imgVal;
    private ArrayList<String> job_listing_category;




/*
    public CategoryBean(String id,String name) {
        this.id = id;
        this.name = name;

    }*/


    public chatBean(String name, String src, String imgVal) {
        this.name = name;
        this.src = src;
        this.imgVal = imgVal;
    }

    public chatBean(String id, String name, String src, String count) {
        this.id = id;
        this.name = name;
        this.src = src;
        this.count = count;
    }

    public String getImgVal() {
        return imgVal;
    }

    public void setImgVal(String imgVal) {
        this.imgVal = imgVal;
    }

    public String getCount() {
        return count;
    }

    public void setCount(String count) {
        this.count = count;
    }

    public String getSrc() {
        return src;
    }

    public void setSrc(String src) {
        this.src = src;
    }

    public String getId ()
    {
        return id;
    }

    public void setId (String id)
    {
        this.id = id;
    }

    public String getName ()
    {
        return name;
    }

    public void setName (String name)
    {
        this.name = name;
    }

    @Override
    public String toString() {
        return name;
    }
}
