package com.xamarin;

import android.os.Bundle;
import android.support.v4.app.Fragment;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

public class SimpleFragment extends Fragment {

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        return inflater.inflate(R.layout.simplefragment, container, false);
    }

    public Fragment getSomething() {
        return null;
    }

    public Fragment getAnotherThing() {
        return null;
    }

    public void setSomething(Fragment fragment) {
    }
}
