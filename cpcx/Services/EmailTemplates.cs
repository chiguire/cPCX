namespace cpcx.Services;

public static class EmailTemplates
{
    public static string ConfirmEmail(string confirmUrl) => Layout(
        title: "Confirm your email address",
        preheader: "Please confirm your DeerPost email address.",
        body: $"""
            <p>Thanks for signing up to DeerPost! Before you can start sending and receiving postcards, we need to verify your email address.</p>
            <p>Click the button below to confirm your email address:</p>
            {ActionButton(confirmUrl, "Confirm email address")}
            <p style="color:#6b7280;font-size:14px;">If you didn't create a DeerPost account, you can safely ignore this email.</p>
            """);

    public static string ResetPassword(string resetUrl) => Layout(
        title: "Reset your password",
        preheader: "You requested a password reset for your DeerPost account.",
        body: $"""
            <p>We received a request to reset the password for your DeerPost account.</p>
            <p>Click the button below to choose a new password:</p>
            {ActionButton(resetUrl, "Reset password")}
            <p style="color:#6b7280;font-size:14px;">If you didn't request a password reset, you can safely ignore this email. Your password will not be changed.</p>
            <p style="color:#6b7280;font-size:14px;">This link will expire in 24 hours.</p>
            """);

    private static string ActionButton(string url, string label) =>
        $"""
        <p style="text-align:center;margin:32px 0;">
          <a href="{url}" style="background-color:#2563eb;color:#ffffff;text-decoration:none;padding:12px 28px;border-radius:6px;font-weight:600;font-size:16px;display:inline-block;">{label}</a>
        </p>
        <p style="color:#6b7280;font-size:12px;">If the button doesn't work, copy and paste this link into your browser:<br/>
          <a href="{url}" style="color:#2563eb;word-break:break-all;">{url}</a>
        </p>
        """;

    private static string Layout(string title, string preheader, string body) =>
        $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>{title}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
          <span style="display:none;max-height:0;overflow:hidden;">{preheader}</span>
          <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f3f4f6;padding:40px 16px;">
            <tr>
              <td align="center">
                <table width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;">
                  <!-- Header -->
                  <tr>
                    <td style="background-color:#2563eb;border-radius:8px 8px 0 0;padding:24px 32px;text-align:center;">
                      <span style="color:#ffffff;font-size:22px;font-weight:700;letter-spacing:-0.5px;">DeerPost</span>
                    </td>
                  </tr>
                  <!-- Body -->
                  <tr>
                    <td style="background-color:#ffffff;padding:32px;color:#111827;font-size:16px;line-height:1.6;">
                      <h1 style="margin:0 0 16px;font-size:20px;font-weight:600;color:#111827;">{title}</h1>
                      {body}
                    </td>
                  </tr>
                  <!-- Footer -->
                  <tr>
                    <td style="background-color:#f9fafb;border-top:1px solid #e5e7eb;border-radius:0 0 8px 8px;padding:20px 32px;text-align:center;color:#9ca3af;font-size:12px;">
                      &copy; DeerPost. Sending postcards the sneaky way.
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
