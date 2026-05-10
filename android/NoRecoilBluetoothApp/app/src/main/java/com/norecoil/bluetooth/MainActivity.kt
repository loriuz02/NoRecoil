package com.norecoil.bluetooth

import android.Manifest
import android.annotation.SuppressLint
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothSocket
import android.content.pm.PackageManager
import android.os.Bundle
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.SeekBar
import android.widget.Spinner
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import java.io.OutputStream
import java.util.UUID

class MainActivity : AppCompatActivity() {
    private val bluetoothAdapter: BluetoothAdapter? = BluetoothAdapter.getDefaultAdapter()
    private var socket: BluetoothSocket? = null
    private var outputStream: OutputStream? = null

    private lateinit var spinner: Spinner
    private lateinit var statusLabel: TextView
    private lateinit var rightSeek: SeekBar
    private lateinit var downSeek: SeekBar
    private lateinit var leftSeek: SeekBar
    private lateinit var upSeek: SeekBar
    private lateinit var delaySeek: SeekBar

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        spinner = findViewById(R.id.deviceSpinner)
        statusLabel = findViewById(R.id.statusLabel)
        rightSeek = findViewById(R.id.rightSeek)
        downSeek = findViewById(R.id.downSeek)
        leftSeek = findViewById(R.id.leftSeek)
        upSeek = findViewById(R.id.upSeek)
        delaySeek = findViewById(R.id.delaySeek)

        refreshDevices()

        findViewById<Button>(R.id.connectButton).setOnClickListener {
            connectSelectedDevice()
        }

        findViewById<Button>(R.id.sendButton).setOnClickListener {
            sendCurrentPreset()
        }
    }

    @SuppressLint("MissingPermission")
    private fun refreshDevices() {
        if (bluetoothAdapter == null) {
            statusLabel.text = "Bluetooth unavailable"
            return
        }

        if (!hasBluetoothPermission()) {
            ActivityCompat.requestPermissions(this, arrayOf(Manifest.permission.BLUETOOTH_CONNECT), 100)
            statusLabel.text = "Permission required"
            return
        }

        val devices = bluetoothAdapter.bondedDevices.toList()
        val names = devices.map { "${it.name ?: "Unknown"} (${it.address})" }

        spinner.adapter = ArrayAdapter(this, android.R.layout.simple_spinner_dropdown_item, names)
        spinner.tag = devices
        statusLabel.text = if (devices.isEmpty()) "No paired devices" else "Ready"
    }

    @SuppressLint("MissingPermission")
    private fun connectSelectedDevice() {
        val devices = spinner.tag as? List<*> ?: return
        val selectedDevice = devices.getOrNull(spinner.selectedItemPosition) as? BluetoothDevice ?: return

        try {
            socket?.close()
            val sppUuid = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB")
            socket = selectedDevice.createRfcommSocketToServiceRecord(sppUuid)
            bluetoothAdapter?.cancelDiscovery()
            socket?.connect()
            outputStream = socket?.outputStream
            statusLabel.text = "Connected to ${selectedDevice.name ?: selectedDevice.address}"
        } catch (ex: Exception) {
            statusLabel.text = "Connect failed: ${ex.message}"
        }
    }

    private fun sendCurrentPreset() {
        val preset = buildString {
            appendLine("[Recoil]")
            appendLine("Right=${rightSeek.progress}")
            appendLine("Down=${downSeek.progress}")
            appendLine("Left=${leftSeek.progress}")
            appendLine("Up=${upSeek.progress}")
            appendLine("Delay=${delaySeek.progress}")
            appendLine()
        }

        try {
            outputStream?.write(preset.toByteArray())
            outputStream?.flush()
            statusLabel.text = "Preset sent"
        } catch (ex: Exception) {
            statusLabel.text = "Send failed: ${ex.message}"
        }
    }

    private fun hasBluetoothPermission(): Boolean =
        ActivityCompat.checkSelfPermission(this, Manifest.permission.BLUETOOTH_CONNECT) == PackageManager.PERMISSION_GRANTED

    override fun onDestroy() {
        super.onDestroy()
        outputStream?.close()
        socket?.close()
    }
}
