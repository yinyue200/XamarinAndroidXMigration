﻿using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.Button;
using AndroidXOnlyLibrary;

namespace AndroidXOnlyApp
{
	[Activity(Label = "AndroidX", Theme = "@style/AppTheme", MainLauncher = true)]
	public class MainActivity : AppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			var actualBase = GetType().BaseType.FullName;
			Console.WriteLine($"\n*** MainActivity.Base => {actualBase}\n");

			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.activity_main);

			var button1 = FindViewById<MaterialButton>(Resource.Id.button1);
			button1.Text = "Go to AndroidX";
			button1.Click += (_, __) => StartActivity(typeof(AndroidXActivity));

			var button2 = FindViewById<MaterialButton>(Resource.Id.button2);
			button2.Text = "Go to Custom";
			button2.Click += (_, __) => StartActivity(typeof(CustomActivity));

			var button3 = FindViewById<MaterialButton>(Resource.Id.button3);
			button3.Text = "Go to Native";
			button3.Click += (_, __) => StartActivity(typeof(NativeActivity));

			var layout = FindViewById<LinearLayout>(Resource.Id.layout);
			layout.AddView(CreateCheckBox(this));
		}

		private static View CreateCheckBox(AndroidX.AppCompat.App.AppCompatActivity activity)
		{
			var checkbox = new AppCompatCheckBox(activity);
			checkbox.Text = "Check Box!";
			return checkbox;
		}
	}

	[Activity(Label = "AndroidX", Theme = "@style/AppTheme")]
	public class AndroidXActivity : AppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			var actualBase = GetType().BaseType.FullName;
			Console.WriteLine($"\n*** AndroidXActivity.Base => {actualBase}\n");

			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.activity_main);
		}
	}

	[Activity(Label = "Custom", Theme = "@style/AppTheme")]
	public class CustomActivity : LibraryActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			var actualBase = GetType().BaseType.FullName;
			Console.WriteLine($"\n*** CustomActivity.Base => {actualBase}\n");

			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.activity_main);
		}
	}

	[Activity(Label = "Native", Theme = "@style/AppTheme")]
	public class NativeActivity : Activity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			var actualBase = GetType().BaseType.FullName;
			Console.WriteLine($"\n*** NativeActivity.Base => {actualBase}\n");

			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.activity_main);
		}
	}
}
