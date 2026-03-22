using Caliburn.Micro;
using DnsCrypt.Models;
using GongSolutions.Wpf.DragDrop;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using DragDropEffects = System.Windows.DragDropEffects;
using IDropTarget = GongSolutions.Wpf.DragDrop.IDropTarget;
using Screen = Caliburn.Micro.Screen;

namespace SimpleDnsCrypt.ViewModels;

[Export(typeof(RouteViewModel))]
[method: ImportingConstructor]
public class RouteViewModel() : Screen, IDropTarget
{
	public string Resolver
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Resolver);
		}
	}

	public ObservableCollection<StampFileEntry> Route
	{
		get;
		set
		{
			field = value;
			NotifyOfPropertyChange(() => Route);
		}
	}

	public BindableCollection<StampFileEntry> Relays { get; internal set; }

	public void Remove(StampFileEntry stampFileEntry)
	{
		if (stampFileEntry != null)
		{
			Route.Remove(stampFileEntry);
		}
	}

	void IDropTarget.DragOver(IDropInfo dropInfo)
	{
		if ((dropInfo.Data is StampFileEntry && dropInfo.TargetItem is StampFileEntry) || dropInfo.TargetItem is null)
		{
			dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
			dropInfo.Effects = DragDropEffects.Move;
		}
	}

	void IDropTarget.Drop(IDropInfo dropInfo)
	{
		StampFileEntry stampFileEntry = (StampFileEntry)dropInfo.Data;
		if (Route.Where(s => s.Name.Equals(stampFileEntry.Name)).FirstOrDefault() != null)
			return;
		Route.Add(stampFileEntry);
	}
}
