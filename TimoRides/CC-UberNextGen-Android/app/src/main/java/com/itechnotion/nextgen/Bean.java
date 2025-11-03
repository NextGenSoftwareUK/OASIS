package com.itechnotion.nextgen;

import java.io.Serializable;

/**
 * Created by cd on 24-10-2017.
 */

public class Bean implements Serializable {

    private String id;
    private String name;
    private String review;
    private String rating;
    private String date;
    private int thumbnail_id;


    public Bean(String name, String review) {
        this.name = name;
        this.review = review;


    }

    public String getDate() {
        return date;
    }

    public void setDate(String date) {
        this.date = date;
    }

    public String getReview() {
        return review;
    }

    public void setReview(String review) {
        this.review = review;
    }

    public String getRating() {
        return rating;
    }

    public void setRating(String rating) {
        this.rating = rating;
    }

    public int getThumbnail_id() {
        return thumbnail_id;
    }

    public void setThumbnail_id(int thumbnail_id) {
        this.thumbnail_id = thumbnail_id;
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
