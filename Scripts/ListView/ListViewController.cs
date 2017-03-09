﻿#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;

namespace ListView
{
	public abstract class ListViewController<DataType, ItemType, IndexType> : ListViewControllerBase, IInstantiateUI, IConnectInterfaces
		where DataType : ListViewItemData<IndexType>
		where ItemType : ListViewItem<DataType, IndexType>
	{
		public virtual List<DataType> data
		{
			get { return m_Data; }
			set
			{
				if (m_Data != null)
				{
					foreach (var item in m_ListItems.Values) // Clear out visuals for old data
					{
						RecycleItem(item.data.template, item);
					}

					m_ListItems.Clear();
				}

				m_Data = value;
				scrollOffset = 0;
			}
		}
		protected List<DataType> m_Data;

		protected readonly Dictionary<IndexType, ItemType> m_ListItems = new Dictionary<IndexType, ItemType>();
		protected readonly Dictionary<IndexType, Transform> m_GrabbedRows = new Dictionary<IndexType, Transform>();

		protected override int dataLength { get { return m_Data.Count; } }

		public InstantiateUIDelegate instantiateUI { private get; set; }
		public ConnectInterfacesDelegate connectInterfaces { private get; set; }

		protected override void UpdateItems()
		{
			m_SettleTest = true;

			for (int i = 0; i < m_Data.Count; i++)
			{
				var datum = m_Data[i];
				if (i + m_DataOffset < -1 || i + m_DataOffset > m_NumRows - 1)
					Recycle(datum.index);
				else
					UpdateVisibleItem(datum, i);
			}

			if (m_Settling && m_SettleTest)
				EndSettling();
		}

		protected virtual void Recycle(IndexType index)
		{
			ItemType item;
			if (m_ListItems.TryGetValue(index, out item))
			{
				RecycleItem(item.data.template, item);
				m_ListItems.Remove(index);
			}
		}

		protected virtual void UpdateVisibleItem(DataType data, int offset)
		{
			ItemType item;
			var index = data.index;
			if (!m_ListItems.TryGetValue(index, out item))
			{
				item = GetItem(data);
				m_ListItems[index] = item;
			}

			UpdateItemTransform(item.transform, offset);
		}

		protected void SetRowGrabbed(IndexType index, Transform rayOrigin, bool grabbed)
		{
			if (grabbed)
				m_GrabbedRows[index] = rayOrigin;
			else
				m_GrabbedRows.Remove(index);
		}

		protected ItemType GetGrabbedRow(Transform rayOrigin)
		{
			foreach (var row in m_GrabbedRows)
			{
				if (row.Value == rayOrigin)
					return GetListItem(row.Key);
			}
			return null;
		}

		protected ItemType GetListItem(IndexType index)
		{
			ItemType item;
			return m_ListItems.TryGetValue(index, out item) ? item : null;
		}

		protected virtual ItemType GetItem(DataType data)
		{
			if (data == null)
			{
				Debug.LogWarning("Tried to get item with null data");
				return null;
			}

			if (!m_TemplateDictionary.ContainsKey(data.template))
			{
				Debug.LogWarning("Cannot get item, template " + data.template + " doesn't exist");
				return null;
			}

			ItemType item;
			if (m_TemplateDictionary[data.template].pool.Count > 0)
			{
				item = (ItemType) m_TemplateDictionary[data.template].pool[0];
				m_TemplateDictionary[data.template].pool.RemoveAt(0);

				item.gameObject.SetActive(true);
				item.Setup(data);
			}
			else
			{
				item = instantiateUI(m_TemplateDictionary[data.template].prefab, transform, false).GetComponent<ItemType>();
				connectInterfaces(item);
				item.Setup(data);
			}

			m_ListItems[data.index] = item;

			item.startSettling = StartSettling;
			item.endSettling = EndSettling;
			item.getListItem = GetListItem;
			item.setRowGrabbed = SetRowGrabbed;
			item.getGrabbedRow = GetGrabbedRow;

			return item;
		}
	}
}
#endif
