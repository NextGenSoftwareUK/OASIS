package com.itechnotion.nextgen.utils;

import android.content.Context;
import android.net.ConnectivityManager;
import android.util.Log;

import java.text.DecimalFormat;
import java.text.DecimalFormatSymbols;
import java.util.Locale;


public class AppConstants {
    public static final boolean IS_WISH_LIST_ACTIVE = true;
    public static final String PRICE = "price";
    public static final String DATE ="date_created" ;
    public static final String PRODNAME ="prod_name" ;
    public static final String QUANTITY = "quantity";
    public static final String TOTAL ="total" ;
    public static final String method_title="method_title";
    public static final String payment_method_title="payment_method_title";
    public static final String ADDRESS = "address";
    public static final String ORDER_STATUS ="order_status" ;
    public static final String PRODID ="ProductId" ;
    public static final String PRODUCTNAME ="ProductName" ;
    public static final String CATID ="Category_id" ;
    public static final String CATNAME ="Category_name" ;
    public static final String PRODDIS ="ProductDescription" ;
    public static final String KEY_FNAME = "fname";
    public static final String KEY_LNAME = "lname";
    public static String NONCE = "Nonce";
    public static String STATUS = "Status";
    public static String STATUS_LOGIN = "Login";
    public static String STATUS_LOGOUT = "Logout";
    public static final String PREF_NAME = "Login Details";
    public static final String KEY_REMEMBER = "remember";
    public static final String KEY_USERNAME = "email";
    public static final String KEY_PASS = "user_pass";
    public static final String KEY_SFNAME = "first_name";
    public static final String KEY_SLNAME = "last_name";
    public static final String KEY_SADD1 = "address_1";
    public static final String KEY_SADD2 = "address_2";
    public static final String KEY_SEMAIL = "email";
    public static final String KEY_SCITY = "city";
    public static final String KEY_SCOUNTRY = "country";
    public static final String KEY_SPOSTCODE = "postcode";
    public static final String KEY_SSTATE = "state";
    public static final String KEY_AMOUNT = "amount";
    public static final String PAYPAL_CLIENT_ID = "Ados4jT1u1Vza2kbOVS-VqMHoIoZd7iFEOdD2-gNAqjclymVGOZ0xWVkk5RzV49LUbRdKsN4ObXP2-5J";

    public static final String PAGE = "page";
    public static final String INCLUDE = "include";

    public static int Decimal = 2;

    public static String CURRENCYSYMBOL= "$";

    public static String CURRENCYSYMBOLPOSTION = "left";

    public static String THOUSANDSSEPRETER = ",";

    public static String DECIMALSEPRETER = ".";


    public static final String KEY_HEADERCOLOR = "headerColor";
    public static final String KEY_FOOTERCOLOR = "footerColor";
    public static final String KEY_PRIMARYCOLOR = "primaryColor";
    public static final String KEY_TOTALAMOUNT= "amounttotal";
    public static final String ORDER_ID= "Order_id";


    public static boolean isNetworkConnected(Context context) {
        ConnectivityManager cm = (ConnectivityManager) context.getSystemService(Context.CONNECTIVITY_SERVICE);
        return cm.getActiveNetworkInfo() != null;
    }



    public static String setDecimal(Double digit) {

        String decimal = "#,##0.";

        if (AppConstants.Decimal == 0) {
            decimal = "#,##0";
        }
        if ((digit == Math.floor(digit)) && !Double.isInfinite(digit)) {
            // integer type
            for (int i = 0; i < AppConstants.Decimal; i++) {
                decimal = decimal + "0";
            }
        } else {
            for (int i = 0; i < AppConstants.Decimal; i++) {
                decimal = decimal + "#";
            }
        }

        DecimalFormatSymbols unusualSymbols = new DecimalFormatSymbols(Locale.US);
        if (AppConstants.Decimal != 0) {
            unusualSymbols.setDecimalSeparator((char) AppConstants.DECIMALSEPRETER.charAt(0));
        }
        unusualSymbols.setGroupingSeparator(AppConstants.THOUSANDSSEPRETER.charAt(0));

//        String strange = "#,##0.000";
        DecimalFormat weirdFormatter = new DecimalFormat(decimal, unusualSymbols);

        weirdFormatter.setGroupingSize(3);

        String data = weirdFormatter.format(digit);
        Log.e("data is ", data + "");


        return data;
    }
}
