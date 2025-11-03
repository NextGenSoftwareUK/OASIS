package com.itechnotion.nextgen.home;

import android.Manifest;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.location.Location;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AppCompatActivity;
import androidx.cardview.widget.CardView;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import androidx.core.view.GravityCompat;
import androidx.drawerlayout.widget.DrawerLayout;

import com.google.android.gms.common.ConnectionResult;
import com.google.android.gms.common.api.GoogleApiClient;
import com.google.android.gms.location.LocationListener;
import com.google.android.gms.location.LocationRequest;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.maps.CameraUpdateFactory;
import com.google.android.gms.maps.GoogleMap;
import com.google.android.gms.maps.OnMapReadyCallback;
import com.google.android.gms.maps.SupportMapFragment;
import com.google.android.gms.maps.model.BitmapDescriptorFactory;
import com.google.android.gms.maps.model.CameraPosition;
import com.google.android.gms.maps.model.LatLng;
import com.google.android.gms.maps.model.Marker;
import com.google.android.gms.maps.model.MarkerOptions;
import com.google.android.material.bottomsheet.BottomSheetBehavior;
import com.itechnotion.nextgen.R;
import com.itechnotion.nextgen.history.HistoryActivity;
import com.itechnotion.nextgen.invitefriend.InviteCodeActivity;
import com.itechnotion.nextgen.loginsignup.LoginActivity;
import com.itechnotion.nextgen.notification.NotificationActivity;
import com.itechnotion.nextgen.payment.MyWalletActivity;
import com.itechnotion.nextgen.setting.SettingActivity;
import com.karumi.dexter.Dexter;
import com.karumi.dexter.MultiplePermissionsReport;
import com.karumi.dexter.PermissionToken;
import com.karumi.dexter.listener.DexterError;
import com.karumi.dexter.listener.PermissionRequest;
import com.karumi.dexter.listener.PermissionRequestErrorListener;
import com.karumi.dexter.listener.multi.MultiplePermissionsListener;

import java.util.List;

import butterknife.BindView;
import butterknife.ButterKnife;
import butterknife.OnClick;
import de.hdodenhof.circleimageview.CircleImageView;

public class HomepageActivity extends AppCompatActivity implements OnMapReadyCallback, LocationListener, GoogleApiClient.ConnectionCallbacks,
        GoogleMap.OnInfoWindowClickListener, GoogleMap.OnMarkerClickListener, GoogleApiClient.OnConnectionFailedListener {
    private static final String TAG = MainActivity.class.getSimpleName();

    @BindView(R.id.btn_bottom_sheet)
    Button btnBottomSheet;

    @BindView(R.id.bottom_sheet)
    LinearLayout layoutBottomSheet;

    BottomSheetBehavior sheetBehavior;
    @BindView(R.id.btn_bottom_sheet_dialog)
    Button btnBottomSheetDialog;
    @BindView(R.id.btn_bottom_sheet_dialog_fragment)
    Button btnBottomSheetDialogFragment;

    @BindView(R.id.edtAddress)
    EditText edtAddress;
    @BindView(R.id.ivCancel)
    ImageView ivCancel;
    @BindView(R.id.llAddress)
    LinearLayout llAddress;
    @BindView(R.id.cvMenu)
    CardView cvMenu;
    @BindView(R.id.tvHome)
    TextView tvHome;
    @BindView(R.id.ll_home)
    LinearLayout llHome;
    @BindView(R.id.lldrawer)
    LinearLayout lldrawer;
    @BindView(R.id.drawerlayout)
    DrawerLayout drawerlayout;

    @BindView(R.id.cvCancel)
    CardView cvCancel;
    @BindView(R.id.profile_image)
    CircleImageView profileImage;
    @BindView(R.id.ll_wallet)
    LinearLayout llWallet;
    @BindView(R.id.ll_history)
    LinearLayout llHistory;
    @BindView(R.id.ll_notification)
    LinearLayout llNotification;
    @BindView(R.id.ll_inviteFrnd)
    LinearLayout llInviteFrnd;
    @BindView(R.id.ll_settings)
    LinearLayout llSettings;
    @BindView(R.id.ll_logout)
    LinearLayout llLogout;

    @BindView(R.id.llCurrentAddress)
    LinearLayout llCurrentAddress;
    @BindView(R.id.tvManci)
    TextView tvManci;
    @BindView(R.id.tvPrem)
    TextView tvPrem;
    @BindView(R.id.tvVstrapur)
    TextView tvVstrapur;
    @BindView(R.id.tvWallet)
    TextView tvWallet;
    @BindView(R.id.tvhistory)
    TextView tvhistory;
    @BindView(R.id.tvNotification)
    TextView tvNotification;
    @BindView(R.id.tvInviteFrnd)
    TextView tvInviteFrnd;
    @BindView(R.id.tvSettings)
    TextView tvSettings;
    @BindView(R.id.tvLogout)
    TextView tvLogout;
    //  private GoogleMap mMap;
    private CameraPosition mCameraPosition;

    private GoogleMap mMap;
    Location mLastLocation;
    Marker mCurrLocationMarker;
    GoogleApiClient mGoogleApiClient;
    LocationRequest mLocationRequest;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_homepage);
        ButterKnife.bind(this);

        SupportMapFragment mapFragment = (SupportMapFragment) getSupportFragmentManager()
                .findFragmentById(R.id.map);
        mapFragment.getMapAsync(this);
        requestStoragePermission();
        sheetBehavior = BottomSheetBehavior.from(layoutBottomSheet);
        final DrawerLayout finalDrawer = drawerlayout;
        if (finalDrawer.isDrawerVisible(GravityCompat.START)) {
            // getWindow().setFlags(WindowManager.LayoutParams.FLAG_LAYOUT_NO_LIMITS, WindowManager.LayoutParams.FLAG_LAYOUT_NO_LIMITS);

            finalDrawer.closeDrawer(GravityCompat.START);
        }
        /**
         * bottom sheet state change listener
         * we are changing button text when sheet changed state
         * */
        sheetBehavior.setBottomSheetCallback(new BottomSheetBehavior.BottomSheetCallback() {
            @Override
            public void onStateChanged(@NonNull View bottomSheet, int newState) {
                switch (newState) {
                    case BottomSheetBehavior.STATE_HIDDEN:
                        break;
                    case BottomSheetBehavior.STATE_EXPANDED: {
                        ivCancel.setVisibility(View.VISIBLE);
                        btnBottomSheet.setText("Close Sheet");
                    }
                    break;
                    case BottomSheetBehavior.STATE_COLLAPSED: {
                        ivCancel.setVisibility(View.GONE);
                        btnBottomSheet.setText("Expand Sheet");
                    }
                    break;
                    case BottomSheetBehavior.STATE_DRAGGING:
                        break;
                    case BottomSheetBehavior.STATE_SETTLING:
                        break;
                }
            }

            @Override
            public void onSlide(@NonNull View bottomSheet, float slideOffset) {

            }
        });
    }
    private void requestStoragePermission() {
        Dexter.withActivity(this)
                .withPermissions(
                        Manifest.permission.ACCESS_FINE_LOCATION)
                .withListener(new MultiplePermissionsListener() {
                    @Override
                    public void onPermissionsChecked(MultiplePermissionsReport report) {
                        // check if all permissions are granted
                        if (report.areAllPermissionsGranted()) {
                            CheckGpsStatus();
                          //  Toast.makeText(getApplicationContext(), "All permissions are granted!", Toast.LENGTH_SHORT).show();
                        }

                        // check for permanent denial of any permission
                        if (report.isAnyPermissionPermanentlyDenied()) {
                            // show alert dialog navigating to Settings

                        }
                    }

                    @Override
                    public void onPermissionRationaleShouldBeShown(List<PermissionRequest> permissions, PermissionToken token) {
                        token.continuePermissionRequest();
                    }
                }).
                withErrorListener(new PermissionRequestErrorListener() {
                    @Override
                    public void onError(DexterError error) {
                        Toast.makeText(getApplicationContext(), "Error occurred! ", Toast.LENGTH_SHORT).show();
                    }
                })
                .onSameThread()
                .check();
    }
    public void CheckGpsStatus() {

        boolean permissionAccessCoarseLocationApproved =
                ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION)
                        == PackageManager.PERMISSION_GRANTED;

        if (permissionAccessCoarseLocationApproved) {
            boolean backgroundLocationPermissionApproved =
                    ActivityCompat.checkSelfPermission(this,
                            Manifest.permission.ACCESS_BACKGROUND_LOCATION)
                            == PackageManager.PERMISSION_GRANTED;

            if (backgroundLocationPermissionApproved) {
                // App can access location both in the foreground and in the background.
                // Start your service that doesn't have a foreground service type
                // defined.
            } else {
                // App can only access location in the foreground. Display a dialog
                // warning the user that your app must have all-the-time access to
                // location in order to function properly. Then, request background
                // location.
                ActivityCompat.requestPermissions(this, new String[] {
                                Manifest.permission.ACCESS_BACKGROUND_LOCATION},
                        1);
            }
        } else {
            // App doesn't have access to the device's location at all. Make full request
            // for permission.
            ActivityCompat.requestPermissions(this, new String[] {
                            Manifest.permission.ACCESS_COARSE_LOCATION,
                            Manifest.permission.ACCESS_BACKGROUND_LOCATION
                    },
                    1);
        }
    }
    @Override
    public void onMapReady(GoogleMap googleMap) {
        /*LatLng sydney = new LatLng(-33.852, 151.211);
        googleMap.addMarker(new MarkerOptions().position(sydney)
                .title("Marker in Sydney"));
        googleMap.moveCamera(CameraUpdateFactory.newLatLng(sydney));*/
        mMap = googleMap;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            if (ContextCompat.checkSelfPermission(this,
                    Manifest.permission.ACCESS_FINE_LOCATION)
                    == PackageManager.PERMISSION_GRANTED) {
                buildGoogleApiClient();
                // mMap.setMyLocationEnabled(true);
            }
        } else {
            buildGoogleApiClient();
            //mMap.setMyLocationEnabled(true);
            //   mMap.getUiSettings().setMyLocationButtonEnabled(true);
        }

    }

    protected synchronized void buildGoogleApiClient() {
        mGoogleApiClient = new GoogleApiClient.Builder(this)
                .addConnectionCallbacks(this)
                .addOnConnectionFailedListener(this)
                .addApi(LocationServices.API).build();
        mGoogleApiClient.connect();
    }

    @OnClick(R.id.btn_bottom_sheet)
    public void toggleBottomSheet() {
        if (sheetBehavior.getState() != BottomSheetBehavior.STATE_EXPANDED) {
            sheetBehavior.setState(BottomSheetBehavior.STATE_EXPANDED);
            btnBottomSheet.setText("Close sheet");
        } else {
            sheetBehavior.setState(BottomSheetBehavior.STATE_COLLAPSED);
            btnBottomSheet.setText("Expand sheet");
        }
    }

    @Override
    public void onConnected(@Nullable Bundle bundle) {
        mLocationRequest = new LocationRequest();
        mLocationRequest.setInterval(1000);
        mLocationRequest.setFastestInterval(1000);
        mLocationRequest.setPriority(LocationRequest.PRIORITY_BALANCED_POWER_ACCURACY);
        if (ContextCompat.checkSelfPermission(this,
                Manifest.permission.ACCESS_FINE_LOCATION)
                == PackageManager.PERMISSION_GRANTED) {
            LocationServices.FusedLocationApi.requestLocationUpdates(mGoogleApiClient, mLocationRequest, this);
        }
    }

    @Override
    public void onConnectionSuspended(int i) {

    }

    @Override
    public void onLocationChanged(Location location) {

        mLastLocation = location;
        if (mCurrLocationMarker != null) {
            mCurrLocationMarker.remove();
        }
        //Place current location marker
        LatLng latLng = new LatLng(location.getLatitude(), location.getLongitude());
        MarkerOptions markerOptions = new MarkerOptions();
        markerOptions.position(latLng);
        markerOptions.title("Current Position");

        markerOptions.icon(BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_GREEN));
        mCurrLocationMarker = mMap.addMarker(markerOptions);

        //move map camera
        mMap.moveCamera(CameraUpdateFactory.newLatLng(latLng));
        mMap.animateCamera(CameraUpdateFactory.zoomTo(11));


        //stop location updates
        if (mGoogleApiClient != null) {
            LocationServices.FusedLocationApi.removeLocationUpdates(mGoogleApiClient, this);
        }

    }

    @Nullable
    @OnClick({R.id.ivCancel,
            R.id.ll_home,
            R.id.llAddress,
            R.id.cvMenu, R.id.ll_settings, R.id.ll_wallet,
            R.id.ll_notification, R.id.ll_inviteFrnd, R.id.ll_history,
            R.id.ll_logout,R.id.llCurrentAddress,R.id.tvManci,R.id.tvPrem,R.id.tvVstrapur})
    public void onViewClicked(View view) {
        Intent intent;
        switch (view.getId()) {
            case R.id.ivCancel:
                edtAddress.setText("");
                break;
                case R.id.ll_home:
                    intent = new Intent(HomepageActivity.this, HomepageActivity.class);
                    startActivity(intent);
                break;
                case R.id.ll_logout:
                    Intent intent1 = new Intent(HomepageActivity.this, LoginActivity.class);
                    intent1.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TASK | Intent.FLAG_ACTIVITY_NEW_TASK);
                    startActivity(intent1);
                break;
            case R.id.llAddress:
                intent = new Intent(HomepageActivity.this, AddressLocationActivity.class);
                startActivity(intent);
                break;
                case R.id.llCurrentAddress:
                intent = new Intent(HomepageActivity.this, AddressLocationActivity.class);
                startActivity(intent);
                break;  case R.id.tvManci:
                intent = new Intent(HomepageActivity.this, AddressLocationActivity.class);
                startActivity(intent);
                break;  case R.id.tvVstrapur:
                intent = new Intent(HomepageActivity.this, AddressLocationActivity.class);
                startActivity(intent);
                break;  case R.id.tvPrem:
                intent = new Intent(HomepageActivity.this, AddressLocationActivity.class);
                startActivity(intent);
                break;
            case R.id.ll_notification:
                intent = new Intent(HomepageActivity.this, NotificationActivity.class);
                startActivity(intent);
                break;
            case R.id.ll_wallet:
                intent = new Intent(HomepageActivity.this, MyWalletActivity.class);
                startActivity(intent);
                break;
            case R.id.ll_settings:
                intent = new Intent(HomepageActivity.this, SettingActivity.class);
                startActivity(intent);
                break;
            case R.id.ll_inviteFrnd:
                intent = new Intent(HomepageActivity.this, InviteCodeActivity.class);
                startActivity(intent);
                break;
            case R.id.ll_history:
                intent = new Intent(HomepageActivity.this, HistoryActivity.class);
                startActivity(intent);
            case R.id.cvMenu:
                final DrawerLayout finalDrawer = drawerlayout;
                if (finalDrawer.isDrawerVisible(GravityCompat.START)) {
                    //  getWindow().setFlags(WindowManager.LayoutParams.FLAG_LAYOUT_NO_LIMITS, WindowManager.LayoutParams.FLAG_LAYOUT_NO_LIMITS);

                    finalDrawer.closeDrawer(GravityCompat.START);
                } else {
                    //  getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);

                    finalDrawer.openDrawer(GravityCompat.START);
                }
                break;
        }
    }

    @Override
    public void onInfoWindowClick(Marker marker) {

    }

    @Override
    public boolean onMarkerClick(Marker marker) {
        return false;
    }

    @Override
    public void onConnectionFailed(@NonNull ConnectionResult connectionResult) {

    }
}
