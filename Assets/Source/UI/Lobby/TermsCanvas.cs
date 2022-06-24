using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TermsCanvas : MonoBehaviour
{
	public static TermsCanvas instance = null;

	public StringTermsTable stringTermsTable;

	public Text groupNameText;
	public Text contentText;

	public GameObject pageGroupObject;
	public Text pageText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
	}

	bool _showTerms;
	int _page;
	//bool _showBackButton;
	public void RefreshInfo(bool terms)
	{
		_showTerms = terms;
		//_showBackButton = showBackButton;
		//backButtonObject.SetActive(showBackButton);
		groupNameText.SetLocalizedText(UIString.instance.GetString(terms ? "GameUI_TermsOfService" : "GameUI_PrivacyPolicy"));

		if (terms)
		{
			_page = 1;
			pageText.text = _page.ToString();
			RefreshPageText();
			pageGroupObject.SetActive(true);
		}
		else
		{
			pageGroupObject.SetActive(false);
			RefreshPageText();
		}
	}

	public void OnClickLeftButton()
	{
		if (_page == 2)
		{
			_page = 1;
			pageText.text = _page.ToString();
			RefreshPageText();
		}
	}

	public void OnClickRightButton()
	{
		if (_page == 1)
		{
			_page = 2;
			pageText.text = _page.ToString();
			RefreshPageText();
		}
	}

	void RefreshPageText()
	{
		string pageStringId = "";
		if (_showTerms)
		{
			if (_page == 1) pageStringId = "GameUI_TermsOfServiceFullOne";
			else pageStringId = "GameUI_TermsOfServiceFullTwo";
		}
		else
		{
			pageStringId = "GameUI_PrivacyPolicyFull";
		}
		contentText.SetLocalizedText(FindTermsString(pageStringId));
	}

	string FindTermsString(string id)
	{
		for (int i = 0; i < stringTermsTable.dataArray.Length; ++i)
		{
			if (stringTermsTable.dataArray[i].id == id)
			{
				if (OptionManager.instance.language == "KOR")
					return stringTermsTable.dataArray[i].kor;
				return stringTermsTable.dataArray[i].eng;
			}
		}
		return "";
	}
}