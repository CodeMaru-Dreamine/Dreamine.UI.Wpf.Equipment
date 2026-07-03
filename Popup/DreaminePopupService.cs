using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Dreamine.UI.Abstractions.Popup;

namespace Dreamine.UI.Wpf.Equipment.Popup
{
	/// <summary>\brief 깜빡임 팝업 서비스 구현.</summary>
	public sealed class DreaminePopupService : IPopupService
	{
		// UI 스레드 전용 — 모든 Add/Remove는 Dispatcher.InvokeAsync 또는 WPF 이벤트 핸들러에서만 수행
		private readonly List<Window> _opened = new();
		private readonly Dictionary<Window, BlinkPopupOptions> _optionsByWindow = new();

		/// <inheritdoc/>
		public bool? ShowBlink(Window? owner, BlinkPopupOptions options)
			=> ShowBlink(owner, options, out _);

		/// <inheritdoc/>
		public bool? ShowBlink(Window? owner, BlinkPopupOptions options, out Window windowRef)
		{
			var win = new DreamineBlinkPopupWindow(options);

			// Owner 설정
			if (owner != null)
			{
				win.Owner = owner;
				win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			}
			else
			{
				var active = GetActive() ?? Application.Current?.MainWindow;
				if (active != null)
				{
					win.Owner = active;
					win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				}
				else
				{
					win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
				}
			}

			// 추적: 창 + 옵션
			_opened.Add(win);
			_optionsByWindow[win] = options;

			// 정리: 창 닫힐 때 제거
			win.Closed += (_, __) =>
			{
				_opened.Remove(win);
				_optionsByWindow.Remove(win);
			};

			windowRef = win;

			// 표시
			if (options.IsModal)
			{
				return win.ShowDialog();
			}
			else
			{
				win.Show();
				return null;
			}
		}

		/// <inheritdoc/>
		public void CloseAll()
		{
			foreach (var w in _opened.ToList())
				if (w.IsLoaded) w.Close();

			_opened.Clear();
			_optionsByWindow.Clear();
		}

		/// <inheritdoc/>
		public void Close(Window window)
		{
			if (window == null) return;
			if (_opened.Contains(window) && window.IsLoaded)
				window.Close();
		}

		/// <inheritdoc/>
		public void CloseOwnedBy(Window owner)
		{
			if (owner == null) return;
			var targets = _opened.Where(w => Equals(w.Owner, owner)).ToList();
			foreach (var w in targets)
				if (w.IsLoaded) w.Close();
		}

		/// <inheritdoc/>
		public Window? GetActive()
			=> _opened.LastOrDefault(w => w.IsVisible && w.IsActive)
			?? _opened.LastOrDefault(w => w.IsVisible);

		/// <inheritdoc/>
		public bool TryGetOptions(Window window, out BlinkPopupOptions? options)
		{
			return _optionsByWindow.TryGetValue(window, out options);
		}

		/// <inheritdoc/>
		public bool TryGetOwnerOptions(Window window, out BlinkPopupOptions? ownerOptions)
		{
			ownerOptions = null;
			if (window?.Owner == null) return false;
			return _optionsByWindow.TryGetValue(window.Owner, out ownerOptions);
		}

		/// <summary>
		/// \brief 외부에서 생성한 팝업 창을 DreaminePopupService 관리 대상에 등록합니다.
		/// Details:
		///  - _opened / _optionsByWindow 에 모두 추가하고,
		///    Closed 이벤트에서 정리까지 수행합니다.
		/// <Param name="window">등록할 팝업 Window.</Param>
		/// <Param name="options">해당 Window에 대응되는 BlinkPopupOptions.</Param>
		/// </summary>
		public void RegisterWindow(Window window, BlinkPopupOptions options)
		{
			if (window is null)
				throw new ArgumentNullException(nameof(window));

			// 중복 등록 방지
			if (_opened.Contains(window))
				return;

			_opened.Add(window);
			_optionsByWindow[window] = options;

			window.Closed += (_, __) =>
			{
				_opened.Remove(window);
				_optionsByWindow.Remove(window);
			};
		}

		// ========================= 비동기 구현 =========================

		/// <inheritdoc/>
		public async Task<bool?> ShowBlinkAsync(
			Window? owner,
			BlinkPopupOptions options,
			TimeSpan? autoCloseAfter = null,
			CancellationToken cancellationToken = default)
		{
			// 모든 UI 조작은 UI 스레드에서
			var dispatcher = (owner ?? Application.Current?.MainWindow)?.Dispatcher
							 ?? Application.Current!.Dispatcher;

			if (dispatcher == null)
				throw new InvalidOperationException("Dispatcher not available.");

			var tcs = new TaskCompletionSource<bool?>(TaskCreationOptions.RunContinuationsAsynchronously);
			Window? win = null;
			bool wasOwnerEnabled = false;

			await dispatcher.InvokeAsync(() =>
			{
				win = new DreamineBlinkPopupWindow(options);

				// Owner 결정
				if (owner != null)
				{
					win.Owner = owner;
					win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				}
				else
				{
					var active = GetActive() ?? Application.Current?.MainWindow;
					if (active != null)
					{
						win.Owner = active;
						win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
					}
					else
					{
						win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
					}
				}

				// 등록
				_opened.Add(win);
				_optionsByWindow[win] = options;

				// 닫힘 시 정리 + 결과 완료
				win.Closed += (_, __) =>
				{
					_opened.Remove(win);
					_optionsByWindow.Remove(win);

					// 모달 효과 복원
					if (options.IsModal && win.Owner != null && wasOwnerEnabled)
						win.Owner.IsEnabled = true;

					// Show()로 열린 경우 DialogResult는 항상 null → PopupResult 사용
					var result = (win as DreamineBlinkPopupWindow)?.PopupResult
					             ?? (win as Window)?.DialogResult;
					_ = tcs.TrySetResult(result);
				};

				// 모달 효과: Owner 잠시 비활성화
				if (options.IsModal && win.Owner != null)
				{
					wasOwnerEnabled = win.Owner.IsEnabled;
					win.Owner.IsEnabled = false;
				}

				// 표시(비동기 모달 시뮬레이션: ShowDialog 대신 Show 사용)
				win.Show();

				// 자동 종료 타이머
				if (autoCloseAfter is TimeSpan delay && delay > TimeSpan.Zero)
				{
					var timer = new DispatcherTimer { Interval = delay };
					timer.Tick += (s, e) =>
					{
						timer.Stop();
						if (win.IsLoaded) win.Close();
					};
					timer.Start();
				}
			});

			// 취소 토큰 연동: 취소 시 UI 스레드에서 닫기
			using (cancellationToken.Register(() =>
			{
				if (win != null)
				{
					_ = dispatcher.InvokeAsync(() =>
					{
						if (win.IsLoaded) win.Close();
					});
				}
			}))
			{
				return await tcs.Task.ConfigureAwait(false);
			}
		}
	}
}
