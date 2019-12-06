using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// カーソルをマウスでもコントローラー&キーボードでも操作できる様にする
/// uGUIをそのまま使用したかったので、:EventSystems.BaseInputModule.PointerInputModuleを継承しました。
/// [Unity 2018.3.6f1]
/// 著作権放棄：ご自由にお使いください：public domain
/// </summary>
namespace CursorController
{
	public class CursorController : PointerInputModule
	{
		[Header("シーン内のカメラをセット")]
		public Camera cameraMain;

		[Header("Axis/ボタン名")]
		public string horizontalAxisString = "Horizontal";
		public string verticalAxisString = "Vertical";
		public string submitKeyString = "Submit";	//決定

		[Header("カーソル移動スピード")]
		public float horizontalSpeed = 1000.0f;
		public float verticalSpeed = 1000.0f;

		[Header("カーソルのRectTransform")]
		public RectTransform cursorRect;

		[Header("画面端判断用Rect")]
		public RectTransform boundaries;

		///<summary> PCマウスカーソル表示制御用 </summary>
		private bool usingDesktopCursor = true;

		///<summary> マウス入力されているか </summary>
		private bool mHasSwitchedToVirtualMouse = false;

		///<summary> コントローラー入力 </summary>
		bool mHasSwitchedToController = false;

		[Header("マウスInputModule")]
		public GameObject mouseInputModule;

		///<summary> カーソル座標 最小値 </summary>
		private Vector2 mMinPos = Vector2.zero;

		///<summary> カーソル座標 最大値 </summary>
		private Vector2 mMaxPos = Vector2.zero;

		///<summary> カーソル移動量 </summary>
		private float mMovementX, mMovementY;

		[Header("カーソルのGameObject")]
		public GameObject cursorObject = null;

		///<summary> PointerEventData生成用 </summary>
		private PointerEventData mPointerEventData;

		[Header("マウスイベント")]
		public EventSystem mouseEventSystem;

		[Header("デバッグ用テキスト")]
		public Text textDebug;

		public Vector2 tmpOffset;

		/// <summary>
		/// 入力デバイス
		/// </summary>
		enum InputState {
			MouseKeyboard,
			Controler
		};
		private InputState mInputState = InputState.MouseKeyboard;

		protected override void Start()
		{
			//カーソル座標制限用座標計算
			mMinPos.x = (boundaries.rect.width / 2f) * -1f;
			mMaxPos.x = (boundaries.rect.width / 2f) - cursorRect.rect.width / 2f;

			mMinPos.y = (boundaries.rect.height / 2f) * -1f + cursorRect.rect.height / 2;
			mMaxPos.y = (boundaries.rect.height / 2f);

			base.Start();
			mPointerEventData = new PointerEventData(eventSystem);
		}

		/// <summary>
		/// ベースクラスのProcessをオーバーライド
		/// </summary>
		public override void Process()
		{
			Vector3 tPos = cursorObject.transform.position;
			Vector3 screenPos = cameraMain.WorldToScreenPoint(tPos);

			mPointerEventData.position = screenPos;

			eventSystem.RaycastAll(mPointerEventData, this.m_RaycastResultCache);
			RaycastResult raycastResult = FindFirstRaycast(this.m_RaycastResultCache);
			mPointerEventData.pointerCurrentRaycast = raycastResult;
			this.ProcessMove(mPointerEventData);

			mPointerEventData.clickCount = 0;
			if( Input.GetButtonDown( submitKeyString ) )
			{
				mPointerEventData.pressPosition = screenPos;
				mPointerEventData.clickTime = Time.unscaledTime;
				mPointerEventData.pointerPressRaycast = raycastResult;

				mPointerEventData.clickCount = 1;
				mPointerEventData.eligibleForClick = true;

				if (this.m_RaycastResultCache.Count > 0)
				{
					mPointerEventData.selectedObject = raycastResult.gameObject;
					mPointerEventData.pointerPress = ExecuteEvents.ExecuteHierarchy(raycastResult.gameObject, mPointerEventData, ExecuteEvents.submitHandler );
					mPointerEventData.rawPointerPress = raycastResult.gameObject;
				}
				else
				{
					mPointerEventData.rawPointerPress = null;
				}
			}
			else
			{
				mPointerEventData.clickCount = 0;
				mPointerEventData.eligibleForClick = false;
				mPointerEventData.pointerPress = null;
				mPointerEventData.rawPointerPress = null;
			}
		}


		void Update()
		{
			//カーソルスピード
			mMovementX = Time.deltaTime * horizontalSpeed * Input.GetAxis(horizontalAxisString);
			mMovementY = Time.deltaTime * verticalSpeed * Input.GetAxis(verticalAxisString);

			if (mouseInputModule)
			{
				if (usingDesktopCursor)
				{
					Cursor.visible = true;	//PCのマウスカーソル表示
					if (!mHasSwitchedToVirtualMouse)
					{
						SwitchToMouse();
					}
				}
				else if (!usingDesktopCursor && !mHasSwitchedToController)
				{
					Cursor.visible = false; //PCのマウスカーソル非表示
					if (!mHasSwitchedToController)
					{
						SwitchToController();
					}
				}
			}
			else if (!mouseInputModule)
			{
				Debug.Log("マウスモジュールを代入してください");
			}

			if (!usingDesktopCursor)
			{
				cursorRect.anchoredPosition += new Vector2(mMovementX, mMovementY);

				Vector3 tPos3;
				tPos3.x = Mathf.Clamp(cursorRect.anchoredPosition.x, mMinPos.x, mMaxPos.x );
				tPos3.y = Mathf.Clamp(cursorRect.anchoredPosition.y, mMinPos.y, mMaxPos.y);
				tPos3.z = 0f;
				cursorRect.anchoredPosition = tPos3;
				cursorRect.transform.localPosition = tPos3;
			}
			else if (usingDesktopCursor)
			{
				cursorRect.position = cameraMain.ScreenToWorldPoint( Input.mousePosition );
			}
		}

		/// <summary>
		/// マウスモードに変更
		/// </summary>
		void SwitchToMouse()
		{
			mHasSwitchedToVirtualMouse = true;
			mHasSwitchedToController = false;
			GetComponent<EventSystem>().enabled = false;
			mouseInputModule.SetActive(true);
			cursorObject.SetActive(false);
		}

		/// <summary>
		/// キーボードモードに変更
		/// </summary>
		void SwitchToController()
		{
			mHasSwitchedToVirtualMouse = false;
			mHasSwitchedToController = true;
			mouseInputModule.SetActive(false);
			GetComponent<EventSystem>().enabled = true;
			cursorObject.SetActive(true);
		}


		/// <summary>
		/// EventはOnGUIの中でのみ受け取れるのでOnGUIで処理
		/// https://docs.unity3d.com/ja/2018.1/Manual/ExecutionOrder.html
		/// </summary>
		void OnGUI()
		{
			//EventはOnGUIの中でのみ受け取れるのでここで実行
			DeviceChangeCheck();
		}

		/// <summary>
		/// デバイスチェック
		/// </summary>
		void DeviceChangeCheck()
		{
			string tStr;

			switch (mInputState)
			{
			case InputState.MouseKeyboard:
				if (isControlerInput())
				{
					mInputState = InputState.Controler;
					usingDesktopCursor = false;
					Debug.Log("モード：マウス");
				}
				tStr = "モード：マウス\n";
				textDebug.text = tStr + Input.mousePosition.ToString();
				break;

			case InputState.Controler:
				if (isMouseKeyboard())
				{
					mInputState = InputState.MouseKeyboard;
					usingDesktopCursor = true;
					Debug.Log("モード：コントローラー&キーボード");
				}
				tStr = "モード：コントローラー&キーボード\n";
				textDebug.text = tStr + mPointerEventData.position.ToString();
				break;
			}
		}

		/// <summary>
		/// マウス入力チェック
		/// </summary>
		/// <returns></returns>
		private bool isMouseKeyboard()
		{
			// マウスのボタン
			if (Event.current.isMouse)
			{
				return true;
			}

			if (Mathf.Abs(Input.GetAxis("Mouse X")) > Mathf.Epsilon ||
				Mathf.Abs(Input.GetAxis("Mouse Y")) > Mathf.Epsilon)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// キーボード＆コントローラーチェック
		/// </summary>
		/// <returns></returns>
		private bool isControlerInput()
		{
			//ジョイスティック1のボタンをチェック
			// ※KeyCode.Joystick1Button19まである
			for (int i = 0; i < 19; i++)
			{
				KeyCode tKeyCode = KeyCode.Joystick1Button0 + i;
				if (Input.GetKey(tKeyCode)){
					return true;
				}
			}

			//ジョイスティック入力
			if (Mathf.Abs(Input.GetAxis( horizontalAxisString )) > Mathf.Epsilon ||
				Mathf.Abs(Input.GetAxis( verticalAxisString )) > Mathf.Epsilon)
			{
				return true;
			}
			return false;
		}
	}

}