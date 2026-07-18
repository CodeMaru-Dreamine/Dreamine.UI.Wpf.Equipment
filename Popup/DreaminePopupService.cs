using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Dreamine.UI.Abstractions.Popup;

namespace Dreamine.UI.Wpf.Equipment.Popup
{
	/// <summary>
	/// \if KO
	/// <para>점멸 팝업 창을 표시·추적·종료하고 동기 및 비동기 결과를 제공합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Displays, tracks, and closes blinking popup windows with synchronous and asynchronous results.</para>
	/// \endif
	/// </summary>
	public sealed class DreaminePopupService : IPopupService
	{
		// UI 스레드 전용 — 모든 Add/Remove는 Dispatcher.InvokeAsync 또는 WPF 이벤트 핸들러에서만 수행
		/// <summary>
		/// \if KO
		/// <para>현재 서비스가 추적하는 열린 창 목록입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores open windows currently tracked by the service.</para>
		/// \endif
		/// </summary>
		private readonly List<Window> _opened = new();
		/// <summary>
		/// \if KO
		/// <para>각 추적 창에 사용된 팝업 옵션을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores popup options associated with each tracked window.</para>
		/// \endif
		/// </summary>
		private readonly Dictionary<Window, BlinkPopupOptions> _optionsByWindow = new();

		/// <summary>
		/// \if KO
		/// <para>점멸 팝업을 표시하고 모달이면 결과를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Shows a blinking popup and returns its result when modal.</para>
		/// \endif
		/// </summary>
		/// <param name="owner">
		/// \if KO
		/// <para>선택적 소유 창입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The optional owner window.</para>
		/// \endif
		/// </param>
		/// <param name="options">
		/// \if KO
		/// <para>팝업 표시 옵션입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Popup display options.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>모달 결과이며 비모달이면 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The modal result, or <see langword="null"/> for a modeless popup.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="options"/>가 <see langword="null"/>일 때 팝업 생성 중 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown during popup creation when <paramref name="options"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public bool? ShowBlink(Window? owner, BlinkPopupOptions options)
			=> ShowBlink(owner, options, out _);

		/// <summary>
		/// \if KO
		/// <para>점멸 팝업을 표시하고 생성된 창 참조와 모달 결과를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Shows a blinking popup and returns both the created window reference and modal result.</para>
		/// \endif
		/// </summary>
		/// <param name="owner">
		/// \if KO
		/// <para>선택적 소유 창입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The optional owner window.</para>
		/// \endif
		/// </param>
		/// <param name="options">
		/// \if KO
		/// <para>팝업 표시 옵션입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Popup display options.</para>
		/// \endif
		/// </param>
		/// <param name="windowRef">
		/// \if KO
		/// <para>생성된 팝업 창입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The created popup window.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>모달 결과이며 비모달이면 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The modal result, or <see langword="null"/> for modeless display.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="options"/>가 <see langword="null"/>일 때 팝업 생성 중 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown during popup creation when <paramref name="options"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
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

		/// <summary>
		/// \if KO
		/// <para>추적 중이며 로드된 모든 팝업 창을 닫고 내부 추적 상태를 비웁니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closes all tracked loaded popup windows and clears internal tracking state.</para>
		/// \endif
		/// </summary>
		public void CloseAll()
		{
			foreach (var w in _opened.ToList())
				if (w.IsLoaded) w.Close();

			_opened.Clear();
			_optionsByWindow.Clear();
		}

		/// <summary>
		/// \if KO
		/// <para>지정한 창이 추적 중이고 로드되어 있으면 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closes the specified window when it is tracked and loaded.</para>
		/// \endif
		/// </summary>
		/// <param name="window">
		/// \if KO
		/// <para>닫을 창입니다. null은 무시됩니다.</para>
		/// \endif
		/// \if EN
		/// <para>The window to close; null is ignored.</para>
		/// \endif
		/// </param>
		public void Close(Window window)
		{
			if (window == null) return;
			if (_opened.Contains(window) && window.IsLoaded)
				window.Close();
		}

		/// <summary>
		/// \if KO
		/// <para>지정한 소유 창에 속한 모든 로드된 팝업을 닫습니다.</para>
		/// \endif
		/// \if EN
		/// <para>Closes every loaded popup owned by the specified window.</para>
		/// \endif
		/// </summary>
		/// <param name="owner">
		/// \if KO
		/// <para>자식 팝업을 닫을 소유 창입니다. null은 무시됩니다.</para>
		/// \endif
		/// \if EN
		/// <para>The owner whose child popups are closed; null is ignored.</para>
		/// \endif
		/// </param>
		public void CloseOwnedBy(Window owner)
		{
			if (owner == null) return;
			var targets = _opened.Where(w => Equals(w.Owner, owner)).ToList();
			foreach (var w in targets)
				if (w.IsLoaded) w.Close();
		}

		/// <summary>
		/// \if KO
		/// <para>활성 추적 창을 우선하고 없으면 마지막 표시 창을 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Returns the active tracked window, falling back to the last visible window.</para>
		/// \endif
		/// </summary>
		/// <returns>
		/// \if KO
		/// <para>활성 또는 표시 팝업이며 없으면 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The active or visible popup, or <see langword="null"/> if none exists.</para>
		/// \endif
		/// </returns>
		public Window? GetActive()
			=> _opened.LastOrDefault(w => w.IsVisible && w.IsActive)
			?? _opened.LastOrDefault(w => w.IsVisible);

		/// <summary>
		/// \if KO
		/// <para>지정한 추적 창에 연결된 팝업 옵션을 가져오려고 시도합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Attempts to get popup options associated with a tracked window.</para>
		/// \endif
		/// </summary>
		/// <param name="window">
		/// \if KO
		/// <para>조회할 창입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The window to query.</para>
		/// \endif
		/// </param>
		/// <param name="options">
		/// \if KO
		/// <para>성공 시 연결된 옵션입니다.</para>
		/// \endif
		/// \if EN
		/// <para>On success, the associated options.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>옵션을 찾았으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> if options were found.</para>
		/// \endif
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="window"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="window"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
		public bool TryGetOptions(Window window, out BlinkPopupOptions? options)
		{
			return _optionsByWindow.TryGetValue(window, out options);
		}

		/// <summary>
		/// \if KO
		/// <para>지정한 창의 소유 창에 연결된 팝업 옵션을 가져오려고 시도합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Attempts to get popup options associated with the specified window's owner.</para>
		/// \endif
		/// </summary>
		/// <param name="window">
		/// \if KO
		/// <para>소유 창을 검사할 창입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The window whose owner is inspected.</para>
		/// \endif
		/// </param>
		/// <param name="ownerOptions">
		/// \if KO
		/// <para>성공 시 소유 창의 옵션입니다.</para>
		/// \endif
		/// \if EN
		/// <para>On success, the owner's options.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>소유 창과 옵션을 찾았으면 <see langword="true"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> if an owner and its options were found.</para>
		/// \endif
		/// </returns>
		public bool TryGetOwnerOptions(Window window, out BlinkPopupOptions? ownerOptions)
		{
			ownerOptions = null;
			if (window?.Owner == null) return false;
			return _optionsByWindow.TryGetValue(window.Owner, out ownerOptions);
		}

		/// <summary>
		/// \if KO
		/// <para>외부에서 만든 팝업 창과 옵션을 추적 대상으로 등록하고 닫힘 시 자동 제거합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Registers an externally created popup window and options for tracking and automatic removal on close.</para>
		/// \endif
		/// </summary>
		/// <param name="window">
		/// \if KO
		/// <para>등록할 팝업 창입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The popup window to register.</para>
		/// \endif
		/// </param>
		/// <param name="options">
		/// \if KO
		/// <para>창에 연결할 팝업 옵션입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The popup options associated with the window.</para>
		/// \endif
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// \if KO
		/// <para><paramref name="window"/>가 <see langword="null"/>일 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when <paramref name="window"/> is <see langword="null"/>.</para>
		/// \endif
		/// </exception>
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

		/// <summary>
		/// \if KO
		/// <para>UI 디스패처에서 팝업을 비동기로 표시하고 닫힘, 자동 종료 또는 취소 후 결과를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Shows a popup asynchronously on the UI dispatcher and returns after closure, automatic timeout, or cancellation.</para>
		/// \endif
		/// </summary>
		/// <param name="owner">
		/// \if KO
		/// <para>선택적 소유 창입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The optional owner window.</para>
		/// \endif
		/// </param>
		/// <param name="options">
		/// \if KO
		/// <para>팝업 표시 옵션입니다.</para>
		/// \endif
		/// \if EN
		/// <para>Popup display options.</para>
		/// \endif
		/// </param>
		/// <param name="autoCloseAfter">
		/// \if KO
		/// <para>선택적 자동 종료 지연 시간입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The optional automatic-close delay.</para>
		/// \endif
		/// </param>
		/// <param name="cancellationToken">
		/// \if KO
		/// <para>창 닫기를 요청할 취소 토큰입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A cancellation token that requests window closure.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>확인·취소 결과이며 결과 없이 닫히면 <see langword="null"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The OK or cancel result, or <see langword="null"/> when closed without a result.</para>
		/// \endif
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// \if KO
		/// <para><paramref name="options"/> 또는 현재 WPF 애플리케이션이 없을 때 발생할 수 있습니다.</para>
		/// \endif
		/// \if EN
		/// <para>May be thrown when <paramref name="options"/> or the current WPF application is unavailable.</para>
		/// \endif
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// \if KO
		/// <para>사용할 UI 디스패처가 없을 때 발생합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Thrown when no UI dispatcher is available.</para>
		/// \endif
		/// </exception>
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
