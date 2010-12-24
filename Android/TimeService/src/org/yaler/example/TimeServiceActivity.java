package org.yaler.example;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;

public class TimeServiceActivity extends Activity {

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);        
        setContentView(R.layout.main);
		startService(new Intent(this, TimeService.class));
    }

}