<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
    <head runat="server">
        <title>Delete Plus</title>
        <link rel='shortcut icon' type='image/x-icon' href='${ImgUrl}favicon.png' />
    </head>
    <body>
        
        <div style="position: relative;">

            <div class="tab-body active">
                <progress id="progBar"></progress>
            </div>

            <div class="controls" style="height: 30px;">
                <div style="width: 180px; height: 30px; display: inline-block; vertical-align: middle;">
                    <input type="checkbox" id="unlink" name="unlink" checked="checked" />
                    <label for="unlink"><span class="text" style="white-space: nowrap;">Break Dependencies</span></label>
                </div>
                <div style="width: 75%; height: 30px; display: inline-block; float: right; text-align: right; vertical-align: middle;">
                    <div class="button disabled" id="delete_all"><span class="text">Delete All</span></div>
                    <div class="button disabled" id="unpublish_all"><span class="text">Unpublish All</span></div>
                    <div class="button disabled" id="go_to_item_location"><span class="text">Go To Location</span></div>
                    <div class="button disabled" id="refresh_items"><span class="text">Refresh</span></div>
                    <div class="button disabled" id="close_window"><span class="text">Close</span></div>
                </div>
            </div>

        </div>

    </body>
</html>