package com.cperos.xr.serialplugin;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.hardware.usb.UsbConstants;          // ← add this
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;

import com.hoho.android.usbserial.driver.CdcAcmSerialDriver;
import com.hoho.android.usbserial.driver.UsbSerialDriver;
import com.hoho.android.usbserial.driver.UsbSerialPort;
import com.hoho.android.usbserial.driver.UsbSerialProber;
import com.hoho.android.usbserial.util.SerialInputOutputManager;

import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import android.util.Log;

public class SerialPlugin {
    private static final String TAG = "SerialPlugin";
    private static final int WRITE_TIMEOUT_MS = 2000; // extend timeout for slower devices
    private static UsbManager usbManager;
    private static UsbSerialPort serialPort;
    private static SerialInputOutputManager ioManager;
    private static final ByteArrayOutputStream buffer = new ByteArrayOutputStream();
    private static final Object bufferLock = new Object();

    private static final String ACTION_USB_PERMISSION =
            "com.cperos.xr.serialplugin.USB_PERMISSION";
    private static PendingIntent permissionIntent;
    private static final BroadcastReceiver permissionReceiver = new BroadcastReceiver() {
        @Override public void onReceive(Context ctx, Intent intent) {
            if (ACTION_USB_PERMISSION.equals(intent.getAction())) {
                synchronized (this) {
                    UsbDevice device = intent.getParcelableExtra(UsbManager.EXTRA_DEVICE);
                    boolean granted = intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false);

                    if (granted && device != null) {
                        Log.d(TAG, "Permission granted for device: " + device);
                        try {
                            // Try opening the connection now that we have permission
                            open();
                        } catch (Exception e) {
                            Log.e(TAG, "Error opening connection after permission granted", e);
                        }
                    } else {
                        Log.d(TAG, "Permission denied for device");
                    }
                }
            }
        }
    };

    private static final SerialInputOutputManager.Listener listener =
            new SerialInputOutputManager.Listener() {
                @Override public void onNewData(byte[] data) {
                    Log.d(TAG, "Received data: " + Arrays.toString(data) + " (" + data.length + " bytes)");
                    String text = new String(data, 0, data.length);
                    Log.d(TAG, "As text: '" + text + "'");
                    synchronized(bufferLock) {
                        buffer.write(data, 0, data.length);
                    }
                }
                @Override public void onRunError(Exception e) {
                    Log.e(TAG, "I/O manager error", e);
                }
            };

    public static void init(Context context) {
        usbManager = (UsbManager) context.getApplicationContext()
                .getSystemService(Context.USB_SERVICE);
        permissionIntent = PendingIntent.getBroadcast(
                context, 0, new Intent(ACTION_USB_PERMISSION),
                PendingIntent.FLAG_MUTABLE | PendingIntent.FLAG_UPDATE_CURRENT
        );
        IntentFilter filter = new IntentFilter(ACTION_USB_PERMISSION);
        context.registerReceiver(permissionReceiver, filter);
    }

    public static boolean open() throws Exception {
        Log.d(TAG, "Attempting to open serial connection");
        List<UsbSerialDriver> drivers =
                UsbSerialProber.getDefaultProber().findAllDrivers(usbManager);

        Log.d(TAG, "Found " + drivers.size() + " drivers");
        if (drivers.isEmpty()) {
            HashMap<String, UsbDevice> deviceList = usbManager.getDeviceList();
            List<UsbSerialDriver> manual = new ArrayList<>();
            for (UsbDevice device : deviceList.values()) {
                for (int i = 0; i < device.getInterfaceCount(); i++) {
                    int cls = device.getInterface(i).getInterfaceClass();
                    if (cls == UsbConstants.USB_CLASS_CDC_DATA ||
                            cls == UsbConstants.USB_CLASS_COMM) {
                        manual.add(new CdcAcmSerialDriver(device));
                        break;
                    }
                }
            }
            drivers = manual;
        }

        if (drivers.isEmpty()) {
            return false;
        }

        UsbSerialDriver driver = drivers.get(0);
        UsbDevice device = driver.getDevice();

        Log.d(TAG, "Using device: " + device.getDeviceName());

        if (!usbManager.hasPermission(device)) {
            usbManager.requestPermission(device, permissionIntent);
            return false;
        }

        UsbDeviceConnection conn = usbManager.openDevice(device);
        if (conn == null) {
            return false;
        }

        serialPort = driver.getPorts().get(0);
        serialPort.open(conn);
        serialPort.setParameters(
                115200,
                8,
                UsbSerialPort.STOPBITS_1,
                UsbSerialPort.PARITY_NONE
        );

        // Clear stale data and give the device a brief settle time
        try {
            serialPort.purgeHwBuffers(true, true);
        } catch (UnsupportedOperationException ignored) {
            Log.w(TAG, "purgeHwBuffers not supported, skipping");
        }
        try { Thread.sleep(50); } catch (InterruptedException ignored) {}

        // Try setting these signals - many devices need them
        serialPort.setDTR(true);
        serialPort.setRTS(true);

        // start background I/O
        ioManager = new SerialInputOutputManager(serialPort, listener);
        ioManager.start();

        Log.d(TAG, "Serial port opened successfully");
        Log.d(TAG, "I/O manager started");

        return true;
    }

    /**
     * Non‐blocking read: returns up to maxBytes from buffer
     */
    public static byte[] read(int maxBytes) {
        synchronized(bufferLock) {
            byte[] all = buffer.toByteArray();
            if (all.length == 0) return new byte[0];
            int len = Math.min(all.length, maxBytes);
            byte[] out = Arrays.copyOfRange(all, 0, len);
            buffer.reset();
            if (all.length > len) {
                buffer.write(all, len, all.length - len);
            }
            return out;
        }
    }

    public static void write(byte[] data) throws Exception {
        if (serialPort != null) {
            try {
                serialPort.write(data, WRITE_TIMEOUT_MS);
            } catch (Exception e) {
                Log.e(TAG, "Write failed, will close port", e);
                close();
                throw e;
            }
        }
    }

    public static void close() throws Exception {
        if (ioManager != null) {
            ioManager.stop();
            ioManager = null;
        }
        if (serialPort != null) {
            serialPort.close();
            serialPort = null;
        }
    }
}
