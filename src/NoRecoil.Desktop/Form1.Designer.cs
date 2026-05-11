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
        toggleRecoilButton = new Button();
        manageKeybindsButton = new Button();
        requireRightMouseCheckbox = new CheckBox();
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
        // toggleRecoilButton
        // 
        toggleRecoilButton.Location = new Point(20, 318);
        toggleRecoilButton.Name = "toggleRecoilButton";
        toggleRecoilButton.Size = new Size(180, 32);
        toggleRecoilButton.TabIndex = 17;
        toggleRecoilButton.Text = "Toggle recoil (enable)";
        toggleRecoilButton.UseVisualStyleBackColor = true;
        toggleRecoilButton.Click += toggleRecoilButton_Click;
        // 
        // manageKeybindsButton
        // 
        manageKeybindsButton.Location = new Point(210, 318);
        manageKeybindsButton.Name = "manageKeybindsButton";
        manageKeybindsButton.Size = new Size(180, 32);
        manageKeybindsButton.TabIndex = 18;
        manageKeybindsButton.Text = "Manage keybinds";
        manageKeybindsButton.UseVisualStyleBackColor = true;
        manageKeybindsButton.Click += setEnableHotkeyButton_Click;
        // 
        // requireRightMouseCheckbox
        // 
        requireRightMouseCheckbox.AutoSize = true;
        requireRightMouseCheckbox.Location = new Point(20, 360);
        requireRightMouseCheckbox.Name = "requireRightMouseCheckbox";
        requireRightMouseCheckbox.Size = new Size(284, 24);
        requireRightMouseCheckbox.TabIndex = 19;
        requireRightMouseCheckbox.Text = "Require right mouse together with left";
        requireRightMouseCheckbox.UseVisualStyleBackColor = true;
        requireRightMouseCheckbox.CheckedChanged += requireRightMouseCheckbox_CheckedChanged;
        // 
        // statusLabel
        // 
        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(367, 324);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(49, 20);
        statusLabel.TabIndex = 21;
        statusLabel.Text = "Status:";
        // 
        // statusValueLabel
        // 
        statusValueLabel.AutoSize = true;
        statusValueLabel.Location = new Point(422, 324);
        statusValueLabel.Name = "statusValueLabel";
        statusValueLabel.Size = new Size(69, 20);
        statusValueLabel.TabIndex = 22;
        statusValueLabel.Text = "Disabled";
        // 
        // pathLabel
        // 
        pathLabel.AutoSize = true;
        pathLabel.Location = new Point(20, 394);
        pathLabel.Name = "pathLabel";
        pathLabel.Size = new Size(69, 20);
        pathLabel.TabIndex = 23;
        pathLabel.Text = "Last path:";
        // 
        // pathValueLabel
        // 
        pathValueLabel.AutoEllipsis = true;
        pathValueLabel.Location = new Point(95, 394);
        pathValueLabel.Name = "pathValueLabel";
        pathValueLabel.Size = new Size(343, 20);
        pathValueLabel.TabIndex = 24;
        pathValueLabel.Text = "-";
        // 
        // bluetoothPortLabel
        // 
        bluetoothPortLabel.AutoSize = true;
        bluetoothPortLabel.Location = new Point(20, 428);
        bluetoothPortLabel.Name = "bluetoothPortLabel";
        bluetoothPortLabel.Size = new Size(105, 20);
        bluetoothPortLabel.TabIndex = 25;
        bluetoothPortLabel.Text = "Bluetooth COM";
        // 
        // bluetoothPortTextbox
        // 
        bluetoothPortTextbox.Location = new Point(131, 425);
        bluetoothPortTextbox.Name = "bluetoothPortTextbox";
        bluetoothPortTextbox.PlaceholderText = "COM5";
        bluetoothPortTextbox.Size = new Size(99, 27);
        bluetoothPortTextbox.TabIndex = 26;
        bluetoothPortTextbox.TextChanged += bluetoothPortTextbox_TextChanged;
        // 
        // connectBluetoothButton
        // 
        connectBluetoothButton.Location = new Point(238, 423);
        connectBluetoothButton.Name = "connectBluetoothButton";
        connectBluetoothButton.Size = new Size(95, 31);
        connectBluetoothButton.TabIndex = 27;
        connectBluetoothButton.Text = "Connect";
        connectBluetoothButton.UseVisualStyleBackColor = true;
        connectBluetoothButton.Click += connectBluetoothButton_Click;
        // 
        // bluetoothStatusLabel
        // 
        bluetoothStatusLabel.AutoSize = true;
        bluetoothStatusLabel.Location = new Point(340, 428);
        bluetoothStatusLabel.Name = "bluetoothStatusLabel";
        bluetoothStatusLabel.Size = new Size(52, 20);
        bluetoothStatusLabel.TabIndex = 28;
        bluetoothStatusLabel.Text = "BT sta:";
        // 
        // bluetoothStatusValueLabel
        // 
        bluetoothStatusValueLabel.AutoSize = true;
        bluetoothStatusValueLabel.Location = new Point(397, 428);
        bluetoothStatusValueLabel.Name = "bluetoothStatusValueLabel";
        bluetoothStatusValueLabel.Size = new Size(97, 20);
        bluetoothStatusValueLabel.TabIndex = 29;
        bluetoothStatusValueLabel.Text = "Disconnected";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 465);
        Controls.Add(bluetoothStatusValueLabel);
        Controls.Add(bluetoothStatusLabel);
        Controls.Add(connectBluetoothButton);
        Controls.Add(bluetoothPortTextbox);
        Controls.Add(bluetoothPortLabel);
        Controls.Add(pathValueLabel);
        Controls.Add(pathLabel);
        Controls.Add(statusValueLabel);
        Controls.Add(statusLabel);
        Controls.Add(requireRightMouseCheckbox);
        Controls.Add(manageKeybindsButton);
        Controls.Add(toggleRecoilButton);
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
    private Button toggleRecoilButton;
    private Button manageKeybindsButton;
    private CheckBox requireRightMouseCheckbox;
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
