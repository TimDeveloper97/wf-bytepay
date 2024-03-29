﻿using ABytepay.Domain;
using ABytepay.Helpers;
using ABytepay.Models;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ABytepay
{
    public partial class Login : Form
    {
        static BaseFirebase _firebase;
        public static string Key = "";
        public static string Email = "";

        public Login()
        {
            InitializeComponent();

            _firebase = new BaseFirebase();
            var account = CRUDHelper.Deserialize();
            if(account != null)
            {
                tbEmail.Text = account.Email;
                tbKey.Text = account.Key;
            }
        }

        #region ======================= Actions ==========================
        private async void btnCheckKey_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbKey.Text))
            {
                try
                {
                    var key = (await _firebase._firebaseDatabase.Child("Keys").OnceAsync<LicenseKey>())
                    .FirstOrDefault(x => x.Object.Key == tbKey.Text);

                    if (key == null)
                        System.Windows.Forms.MessageBox.Show("License key doesn't exist", "Error");
                    else if (key.Object.IsUse)
                        System.Windows.Forms.MessageBox.Show("License key is already used", "Error");
                    else if (key.Object.End < DateTime.Now)
                        System.Windows.Forms.MessageBox.Show("License key is out of date", "Error");
                    else
                        System.Windows.Forms.MessageBox.Show("License key ready to use", "Success");
                }
                catch (Exception)
                {
                    System.Windows.Forms.MessageBox.Show("No internet", "Error");
                }
            }
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbKey.Text) && !string.IsNullOrEmpty(tbEmail.Text))
            {
                CRUDHelper.Serialize(new Account { Email = tbEmail.Text, Key = tbKey.Text });

                try
                {
                    var key = (await _firebase._firebaseDatabase.Child("Keys").OnceAsync<LicenseKey>())
                    .FirstOrDefault(x => x.Object.Key == tbKey.Text);

                    if (key == null)
                        System.Windows.Forms.MessageBox.Show("License key doesn't exist", "Error");
                    else if (key.Object.Email != null && key.Object.Email != tbEmail.Text)
                        System.Windows.Forms.MessageBox.Show("License key is use in another email", "Error");
                    else if (key.Object.End < DateTime.Now)
                        System.Windows.Forms.MessageBox.Show("License key is out of date", "Error");
                    else
                    {
                        var user = (await _firebase._firebaseDatabase.Child("Users").OnceAsync<User>())
                                    .FirstOrDefault(x => x.Object.Email == tbEmail.Text);

                        var mdevice = ComputerHelper.GetDeviceId();

                        if (user == null)
                        {
                            System.Windows.Forms.MessageBox.Show("Email doesn't exist", "Error");
                            var confirmResult = MessageBox.Show("Do you want create account with this email?",
                                         "Information",
                                         MessageBoxButtons.YesNo);

                            if (confirmResult == DialogResult.Yes)
                            {
                                var u = new User
                                {
                                    ComputerId = mdevice,
                                    Email = tbEmail.Text,
                                    Keys = new List<string>(),
                                    Products = new List<Product>(),
                                };
                                await _firebase._firebaseDatabase.Child("Users").PostAsync(u);
                            }
                        }
                        //else if (user.Object.ComputerId != null && mdevice != user.Object.ComputerId)
                        //    System.Windows.Forms.MessageBox.Show("Email is use in another machine", "Error");
                        //else if(user.Object.Email != null && user.Object.Email != tbEmail.Text)
                        //    System.Windows.Forms.MessageBox.Show("License key is use in another email", "Error");
                        else
                        {
                            var isKeyExist = user.Object.Keys?.Any(x => x == tbKey.Text);
                            if (isKeyExist == null || isKeyExist == false)
                            {
                                var confirmResult = MessageBox.Show("Do you want add key to this email?",
                                         "Information",
                                         MessageBoxButtons.YesNo);

                                if (confirmResult == DialogResult.Yes)
                                {
                                    if (user.Object.Keys == null)
                                        user.Object.Keys = new List<string>();
                                    user.Object.Keys.Add(tbKey.Text);

                                    key.Object.IsUse = true;
                                    key.Object.Email = tbEmail.Text;

                                    await _firebase._firebaseDatabase.Child("Keys").Child(key.Key).PutAsync(key.Object);
                                    await _firebase._firebaseDatabase.Child("Users").Child(user.Key).PutAsync(user.Object);
                                }
                            }
                            else if (isKeyExist == true)
                            {
                                Key = tbKey.Text;
                                Email = tbEmail.Text;

                                var main = new Main(this);
                                main.Show();
                                this.Hide();
                            }
                        }

                    }
                }
                catch (Exception)
                {
                    System.Windows.Forms.MessageBox.Show("No internet", "Error");
                }


            }
        }
        #endregion


    }
}
