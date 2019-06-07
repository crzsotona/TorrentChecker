﻿Public Class frmAppOptions

    Private Sub frmAppOptions_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then Me.Hide()
    End Sub

    Private Sub frmAppOptions_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Icon = TRAY_ICON

        chkHideToTray.Checked = AppOptions("hide_to_tray")
        chkCloseToTray.Checked = AppOptions("close_to_tray")
        chkRunWithWindows.Checked = False
        chkRunHidden.Checked = AppOptions("run_hidden")
        chkShowJournalTab.Checked = AppOptions("show_journal_tab")
        chkAutoChecking.Checked = AppOptions("auto_cheking")
        chkWriteFatalErrors.Checked = AppOptions("write_fatal_errors")
        chkConfirmExit.Checked = AppOptions("confirm_exit")
        txtCheckInterval.Text = CStr(CHECK_INTERVAL / 60000)
        txtTorrentsStoreTime.Text = CStr(TORRENTS_MAX_STORETIME / (24 * 60 * 60))
        txtMaxVisibleResults.Text = CStr(TORRENTS_MAX_RESULTS)

        Try
            If My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", False).GetValue(Application.ProductName) IsNot Nothing Then
                chkRunWithWindows.Checked = True
            End If
        Catch ex As Exception
            chkRunWithWindows.Checked = False
        End Try
    End Sub

    Private Sub txtCheckInterval_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtCheckInterval.KeyPress
        If Not Char.IsDigit(e.KeyChar) And Not Char.IsControl(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    Public Sub New()
        InitializeComponent()

        For Each ctrl As Control In Me.Controls
            ctrl.Font = FONT_NORMAL
        Next

        lblClearCache.Font = FONT_UNDERLINE

        tlpToolTip.SetToolTip(txtTorrentsStoreTime, "Информация о каждом найденном торренте должна быть сохранена в общем кэше, чтобы программа его ""помнила"" и не выводила в следующий раз." & vbCrLf & _
                              "Данный параметр позволяет настроить максимальный срок, после которого информация будет удалена из кэша, когда данный торрент выйдет из области видимости программы")
        tlpToolTip.SetToolTip(txtMaxVisibleResults, "Автоочистка окна найденных торрентов для каждого задания. Применяется каждый раз, когда найден хотя бы один торрент в рамках каждого ключевого слова")
        tlpToolTip.SetToolTip(txtCheckInterval, "Позволяет задать частоту проверок ключевых слов. Изменение вступает в силу при следующей проверке")
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Hide()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If Not MakeChecks() Then Exit Sub

        AppOptions("hide_to_tray") = chkHideToTray.Checked
        AppOptions("close_to_tray") = chkCloseToTray.Checked
        AppOptions("run_hidden") = chkRunHidden.Checked
        AppOptions("show_journal_tab") = chkShowJournalTab.Checked
        AppOptions("auto_cheking") = chkAutoChecking.Checked
        AppOptions("write_fatal_errors") = chkWriteFatalErrors.Checked
        AppOptions("confirm_exit") = chkConfirmExit.Checked
        CHECK_INTERVAL = CInt(txtCheckInterval.Text) * 60000
        TORRENTS_MAX_STORETIME = CLng(txtTorrentsStoreTime.Text) * 24 * 60 * 60
        TORRENTS_MAX_RESULTS = CShort(txtMaxVisibleResults.Text)

        Try
            If chkRunWithWindows.Checked Then
                My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).SetValue(Application.ProductName, Application.ExecutablePath)
            Else
                If My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", False).GetValue(Application.ProductName) IsNot Nothing Then
                    My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).DeleteValue(Application.ProductName)
                End If
            End If
        Catch ex As Exception
            MsgBox("Ошибка при работе с реестром!" & vbCrLf & GetProperExceptionText(ex), MsgBoxStyle.Critical + MsgBoxStyle.OkOnly)
        Finally
            SaveSettings()
            Me.Hide()
        End Try
    End Sub

    Private Function MakeChecks() As Boolean
        Dim x As Integer 'dummy variable for Integer.TryParse

        If Not Integer.TryParse(txtCheckInterval.Text, x) Then
            MsgBox("Неправильный интервал проверки. Он должен состоять только из цифр", MsgBoxStyle.Critical + MsgBoxStyle.OkOnly)
            txtCheckInterval.SelectAll()
            txtCheckInterval.Focus()
            Return False
        End If

        If CInt(txtCheckInterval.Text) < 5 Then
            MsgBox("Слишком маленький интервал проверки. Он должен быть как минимум 5 минут", MsgBoxStyle.Critical + MsgBoxStyle.OkOnly)
            txtCheckInterval.SelectAll()
            txtCheckInterval.Focus()
            Return False
        End If

        If Not Integer.TryParse(txtTorrentsStoreTime.Text, x) Then
            MsgBox("Неправильный срок хранения. Он должен состоять только из цифр", MsgBoxStyle.Critical + MsgBoxStyle.OkOnly)
            txtTorrentsStoreTime.SelectAll()
            txtTorrentsStoreTime.Focus()
            Return False
        End If

        If CInt(txtCheckInterval.Text) = 0 Then
            MsgBox("Некорректный срок хранения. Он должен быть как минимум 1 день", MsgBoxStyle.Critical + MsgBoxStyle.OkOnly)
            txtCheckInterval.SelectAll()
            txtCheckInterval.Focus()
            Return False
        End If

        If chkAutoChecking.Checked AndAlso TrackerParams(Trackers.rutracker)("credentials")("login_username") = "" AndAlso TrackerParams(Trackers.kinozal)("credentials")("username") = "" Then
            MsgBox("Невозможно включить автопроверку при старте, заполните регистрационные данные хотя бы для одного трекера!", MsgBoxStyle.Exclamation + MsgBoxStyle.OkOnly)
            chkAutoChecking.Checked = False
            Return False
        End If

        If chkAutoChecking.Checked AndAlso UBound(KwListToParamsMapping) < 1 Then
            chkAutoChecking.Checked = False
            MsgBox("Невозможно включить автопроверку при старте, добавьте как минимум одно ключевое слово!", MsgBoxStyle.Exclamation + MsgBoxStyle.OkOnly)
            Return False
        End If

        If Not Integer.TryParse(txtMaxVisibleResults.Text, x) Then
            MsgBox("Неправильное ограничение отображаемых результатов. Оно должно состоять только из цифр", MsgBoxStyle.Critical + MsgBoxStyle.OkOnly)
            txtMaxVisibleResults.SelectAll()
            txtMaxVisibleResults.Focus()
            Return False
        End If

        If CInt(txtMaxVisibleResults.Text) = 0 Then
            MsgBox("Некорректное ограничение отображаемых результатов. Оно должно быть как минимум 1 результат", MsgBoxStyle.Critical + MsgBoxStyle.OkOnly)
            txtMaxVisibleResults.SelectAll()
            txtMaxVisibleResults.Focus()
            Return False
        End If

        Return True
    End Function

    Private Sub txtCheckInterval_LostFocus(sender As Object, e As EventArgs) Handles txtCheckInterval.LostFocus
        If txtCheckInterval.TextLength = 0 Then txtCheckInterval.Text = CStr(CHECK_INTERVAL / 60000)
    End Sub

    Private Sub txtTorrentsStoreTime_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtTorrentsStoreTime.KeyPress
        If Not Char.IsDigit(e.KeyChar) And Not Char.IsControl(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    Private Sub txtTorrentsStoreTime_LostFocus(sender As Object, e As EventArgs) Handles txtTorrentsStoreTime.LostFocus
        If txtTorrentsStoreTime.TextLength = 0 Then txtTorrentsStoreTime.Text = CStr(TORRENTS_MAX_STORETIME / (24 * 60 * 60))
    End Sub

    Private Sub txtMaxVisibleResults_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtMaxVisibleResults.KeyPress
        If Not Char.IsDigit(e.KeyChar) And Not Char.IsControl(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    Private Sub txtMaxVisibleResults_LostFocus(sender As Object, e As EventArgs) Handles txtMaxVisibleResults.LostFocus
        If txtMaxVisibleResults.TextLength = 0 Then txtMaxVisibleResults.Text = TORRENTS_MAX_RESULTS
    End Sub

    Private Sub lblClearCache_Click(sender As Object, e As EventArgs) Handles lblClearCache.Click
        If Not MsgBox("Действительно хотите очистить кэш всех найденных торрентов? Они будут найдены повторно", MsgBoxStyle.Question + MsgBoxStyle.YesNo + vbDefaultButton2) = MsgBoxResult.Yes Then
            Exit Sub
        End If
        dtStoredTorrents.Clear()
        MsgBox("Кэш очищен!", MsgBoxStyle.Information + MsgBoxStyle.OkOnly)
    End Sub
End Class