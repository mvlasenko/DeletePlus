<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
    <head runat="server">
        <title>Delete Plus</title>
        <link rel='shortcut icon' type='image/x-icon' href='${ImgUrl}favicon.png' />
    </head>
    <body>
        <div class="tabs">
            <div class="active">Loading...</div>
        </div>
        <div class="tab-body active">
            <progress id="progBar"></progress>
        </div>
        <div class="controls">
            <div class="button disabled" id="delete_item"><span class="text">Delete All</span></div>
            <div class="button disabled" id="unpublish_item"><span class="text">Unpublish All</span></div>
            <div class="button disabled" id="go_to_item_location"><span class="text">Go To Location</span></div>
            <div class="button disabled" id="refresh_items"><span class="text">Refresh</span></div>
            <div class="button disabled" id="close_window"><span class="text">Close</span></div>
        </div>
    </body>
</html>