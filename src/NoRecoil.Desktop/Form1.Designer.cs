namespace NoRecoil.Desktop;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        rightSlider = new TrackBar();
        downSlider = new TrackBar();
        leftSlider = new TrackBar();
        upSlider = new TrackBar();
        delaySlider = new TrackBar();
        rightLabel = new Label();
        downLabel = new Label();
        leftLabel = new Label();
        upLabel = new Label();
        delayLabel = new Label();
        rightValueLabel = new Label();
        downValueLabel = new Label();
        leftValueLabel = new Label();
        upValueLabel = new Label();
        delayValueLabel = new Label();
        saveButton = new Button();
        loadButton = new Button();
        recoilEnabledCheckbox = new CheckBox();
        statusLabel = new Label();
        statusValueLabel = new Label();
        pathLabel = new Label();
        pathValueLabel = new Label();
        bluetoothPortLabel = new Label();
        bluetoothPortTextbox = new TextBox();
        connectBluetoothButton = new Button();
        bluetoothStatusLabel = new Label();
        bluetoothStatusValueLabel = new Label();
        ((System.ComponentModel.ISupportInitialize)rightSlider).BeginInit();
        ((System.ComponentModel.ISupportInitialize)downSlider).BeginInit();
        ((System.ComponentModel.ISupportInitialize)leftSlider).BeginInit();
        ((System.ComponentModel.ISupportInitialize)upSlider).BeginInit();
        ((System.ComponentModel.ISupportInitialize)delaySlider).BeginInit();
        SuspendLayout();
        // 
        // rightSlider
        // 
        rightSlider.Location = new Point(80, 16);
        rightSlider.Maximum = 100;
        rightSlider.Name = "rightSlider";
        rightSlider.Size = new Size(300, 56);
        rightSlider.TabIndex = 0;
        rightSlider.ValueChanged += slider_ValueChanged;
        // 
        // downSlider
        // 
        downSlider.Location = new Point(80, 66);
        downSlider.Maximum = 100;
        downSlider.Name = "downSlider";
        downSlider.Size = new Size(300, 56);
        downSlider.TabIndex = 1;
        downSlider.ValueChanged += slider_ValueChanged;
        // 
        // leftSlider
        // 
        leftSlider.Location = new Point(80, 116);
        leftSlider.Maximum = 100;
        leftSlider.Name = "leftSlider";
        leftSlider.Size = new Size(300, 56);
        leftSlider.TabIndex = 2;
        leftSlider.ValueChanged += slider_ValueChanged;
        // 
        // upSlider
        // 
        upSlider.Location = new Point(80, 166);
        upSlider.Maximum = 100;
        upSlider.Name = "upSlider";
        upSlider.Size = new Size(300, 56);
        upSlider.TabIndex = 3;
        upSlider.ValueChanged += slider_ValueChanged;
        // 
        // delaySlider
        // 
        delaySlider.Location = new Point(80, 216);
        delaySlider.Maximum = 200;
        delaySlider.Name = "delaySlider";
        delaySlider.Size = new Size(300, 56);
        delaySlider.TabIndex = 4;
        delaySlider.ValueChanged += slider_ValueChanged;
        // 
        // rightLabel
        // 
        rightLabel.AutoSize = true;
        rightLabel.Location = new Point(20, 22);
        rightLabel.Name = "rightLabel";
        rightLabel.Size = new Size(42, 20);
        rightLabel.TabIndex = 5;
        rightLabel.Text = "Right";
        // 
        // downLabel
        // 
        downLabel.AutoSize = true;
        downLabel.Location = new Point(20, 72);
        downLabel.Name = "downLabel";
        downLabel.Size = new Size(46, 20);
        downLabel.TabIndex = 6;
        downLabel.Text = "Down";
        // 
        // leftLabel
        // 
        leftLabel.AutoSize = true;
        leftLabel.Location = new Point(20, 122);
        leftLabel.Name = "leftLabel";
        leftLabel.Size = new Size(32, 20);
        leftLabel.TabIndex = 7;
        leftLabel.Text = "Left";
        // 
        // upLabel
        // 
        upLabel.AutoSize = true;
        upLabel.Location = new Point(20, 172);
        upLabel.Name = "upLabel";
        upLabel.Size = new Size(27, 20);
        upLabel.TabIndex = 8;
        upLabel.Text = "Up";
        // 
        // delayLabel
        // 
        delayLabel.AutoSize = true;
        delayLabel.Location = new Point(20, 222);
        delayLabel.Name = "delayLabel";
        delayLabel.Size = new Size(45, 20);
        delayLabel.TabIndex = 9;
        delayLabel.Text = "Delay";
        // 
        // rightValueLabel
        // 
        rightValueLabel.AutoSize = true;
        rightValueLabel.Location = new Point(390, 22);
        rightValueLabel.Name = "rightValueLabel";
        rightValueLabel.Size = new Size(17, 20);
        rightValueLabel.TabIndex = 10;
        rightValueLabel.Text = "0";
        // 
        // downValueLabel
        // 
        downValueLabel.AutoSize = true;
        downValueLabel.Location = new Point(390, 72);
        downValueLabel.Name = "downValueLabel";
        downValueLabel.Size = new Size(17, 20);
        downValueLabel.TabIndex = 11;
        downValueLabel.Text = "0";
        // 
        // leftValueLabel
        // 
        leftValueLabel.AutoSize = true;
        leftValueLabel.Location = new Point(390, 122);
        leftValueLabel.Name = "leftValueLabel";
        leftValueLabel.Size = new Size(17, 20);
        leftValueLabel.TabIndex = 12;
        leftValueLabel.Text = "0";
        // 
        // upValueLabel
        // 
        upValueLabel.AutoSize = true;
        upValueLabel.Location = new Point(390, 172);
        upValueLabel.Name = "upValueLabel";
        upValueLabel.Size = new Size(17, 20);
        upValueLabel.TabIndex = 13;
        upValueLabel.Text = "0";
        // 
        // delayValueLabel
        // 
        delayValueLabel.AutoSize = true;
        delayValueLabel.Location = new Point(390, 222);
        delayValueLabel.Name = "delayValueLabel";
        delayValueLabel.Size = new Size(48, 20);
        delayValueLabel.TabIndex = 14;
        delayValueLabel.Text = "0 ms";
        // 
        // saveButton
        // 
        saveButton.Location = new Point(20, 276);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(180, 35);
        saveButton.TabIndex = 15;
        saveButton.Text = "Save preset";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += saveButton_Click;
        // 
        // loadButton
        // 
        loadButton.Location = new Point(210, 276);
        loadButton.Name = "loadButton";
        loadButton.Size = new Size(170, 35);
        loadButton.TabIndex = 16;
        loadButton.Text = "Load preset";
        loadButton.UseVisualStyleBackColor = true;
        loadButton.Click += loadButton_Click;
        // 
        // recoilEnabledCheckbox
        // 
        recoilEnabledCheckbox.AutoSize = true;
        recoilEnabledCheckbox.Location = new Point(20, 322);
        recoilEnabledCheckbox.Name = "recoilEnabledCheckbox";
        recoilEnabledCheckbox.Size = new Size(127, 24);
        recoilEnabledCheckbox.TabIndex = 17;
        recoilEnabledCheckbox.Text = "Enable recoil";
        recoilEnabledCheckbox.UseVisualStyleBackColor = true;
        recoilEnabledCheckbox.CheckedChanged += recoilEnabledCheckbox_CheckedChanged;
        // 
        // statusLabel
        // 
        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(170, 323);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(49, 20);
        statusLabel.TabIndex = 18;
        statusLabel.Text = "Status:";
        // 
        // statusValueLabel
        // 
        statusValueLabel.AutoSize = true;
        statusValueLabel.Location = new Point(225, 323);
        statusValueLabel.Name = "statusValueLabel";
        statusValueLabel.Size = new Size(69, 20);
        statusValueLabel.TabIndex = 19;
        statusValueLabel.Text = "Disabled";
        // 
        // pathLabel
        // 
        pathLabel.AutoSize = true;
        pathLabel.Location = new Point(20, 355);
        pathLabel.Name = "pathLabel";
        pathLabel.Size = new Size(69, 20);
        pathLabel.TabIndex = 20;
        pathLabel.Text = "Last path:";
        // 
        // pathValueLabel
        // 
        pathValueLabel.AutoEllipsis = true;
        pathValueLabel.Location = new Point(95, 355);
        pathValueLabel.Name = "pathValueLabel";
        pathValueLabel.Size = new Size(343, 20);
        pathValueLabel.TabIndex = 21;
        pathValueLabel.Text = "-";
        // 
        // bluetoothPortLabel
        // 
        bluetoothPortLabel.AutoSize = true;
        bluetoothPortLabel.Location = new Point(20, 389);
        bluetoothPortLabel.Name = "bluetoothPortLabel";
        bluetoothPortLabel.Size = new Size(105, 20);
        bluetoothPortLabel.TabIndex = 22;
        bluetoothPortLabel.Text = "Bluetooth COM";
        // 
        // bluetoothPortTextbox
        // 
        bluetoothPortTextbox.Location = new Point(131, 386);
        bluetoothPortTextbox.Name = "bluetoothPortTextbox";
        bluetoothPortTextbox.PlaceholderText = "COM5";
        bluetoothPortTextbox.Size = new Size(99, 27);
        bluetoothPortTextbox.TabIndex = 23;
        // 
        // connectBluetoothButton
        // 
        connectBluetoothButton.Location = new Point(238, 384);
        connectBluetoothButton.Name = "connectBluetoothButton";
        connectBluetoothButton.Size = new Size(95, 31);
        connectBluetoothButton.TabIndex = 24;
        connectBluetoothButton.Text = "Connect";
        connectBluetoothButton.UseVisualStyleBackColor = true;
        connectBluetoothButton.Click += connectBluetoothButton_Click;
        // 
        // bluetoothStatusLabel
        // 
        bluetoothStatusLabel.AutoSize = true;
        bluetoothStatusLabel.Location = new Point(340, 389);
        bluetoothStatusLabel.Name = "bluetoothStatusLabel";
        bluetoothStatusLabel.Size = new Size(52, 20);
        bluetoothStatusLabel.TabIndex = 25;
        bluetoothStatusLabel.Text = "BT sta:";
        // 
        // bluetoothStatusValueLabel
        // 
        bluetoothStatusValueLabel.AutoSize = true;
        bluetoothStatusValueLabel.Location = new Point(397, 389);
        bluetoothStatusValueLabel.Name = "bluetoothStatusValueLabel";
        bluetoothStatusValueLabel.Size = new Size(97, 20);
        bluetoothStatusValueLabel.TabIndex = 26;
        bluetoothStatusValueLabel.Text = "Disconnected";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 430);
        Controls.Add(bluetoothStatusValueLabel);
        Controls.Add(bluetoothStatusLabel);
        Controls.Add(connectBluetoothButton);
        Controls.Add(bluetoothPortTextbox);
        Controls.Add(bluetoothPortLabel);
        Controls.Add(pathValueLabel);
        Controls.Add(pathLabel);
        Controls.Add(statusValueLabel);
        Controls.Add(statusLabel);
        Controls.Add(recoilEnabledCheckbox);
        Controls.Add(loadButton);
        Controls.Add(saveButton);
        Controls.Add(delayValueLabel);
        Controls.Add(upValueLabel);
        Controls.Add(leftValueLabel);
        Controls.Add(downValueLabel);
        Controls.Add(rightValueLabel);
        Controls.Add(delayLabel);
        Controls.Add(upLabel);
        Controls.Add(leftLabel);
        Controls.Add(downLabel);
        Controls.Add(rightLabel);
        Controls.Add(delaySlider);
        Controls.Add(upSlider);
        Controls.Add(leftSlider);
        Controls.Add(downSlider);
        Controls.Add(rightSlider);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Name = "Form1";
        Text = "NoRecoil Preset Controller";
        ((System.ComponentModel.ISupportInitialize)rightSlider).EndInit();
        ((System.ComponentModel.ISupportInitialize)downSlider).EndInit();
        ((System.ComponentModel.ISupportInitialize)leftSlider).EndInit();
        ((System.ComponentModel.ISupportInitialize)upSlider).EndInit();
        ((System.ComponentModel.ISupportInitialize)delaySlider).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TrackBar rightSlider;
    private TrackBar downSlider;
    private TrackBar leftSlider;
    private TrackBar upSlider;
    private TrackBar delaySlider;
    private Label rightLabel;
    private Label downLabel;
    private Label leftLabel;
    private Label upLabel;
    private Label delayLabel;
    private Label rightValueLabel;
    private Label downValueLabel;
    private Label leftValueLabel;
    private Label upValueLabel;
    private Label delayValueLabel;
    private Button saveButton;
    private Button loadButton;
    private CheckBox recoilEnabledCheckbox;
    private Label statusLabel;
    private Label statusValueLabel;
    private Label pathLabel;
    private Label pathValueLabel;
    private Label bluetoothPortLabel;
    private TextBox bluetoothPortTextbox;
    private Button connectBluetoothButton;
    private Label bluetoothStatusLabel;
    private Label bluetoothStatusValueLabel;
}
