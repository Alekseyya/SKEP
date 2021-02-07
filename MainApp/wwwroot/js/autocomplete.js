var SLOW_LIMIT = 100;
var MIN_LENGTH = 3;

$(function () {
    $.widget("custom.combobox", {
        _create: function () {            
            this.wrapper = $("<div>")
              .addClass("input-group custom-combobox")
              .insertAfter(this.element);

            this.element.hide();
            this._createAutocomplete();
            this._createShowAllButton();
            if (this.element.get(0).length > SLOW_LIMIT) {
                this.input.data("uiAutocomplete").options.minLength = MIN_LENGTH;
                this._createInfoMessage();
            }
        },

        _createInfoMessage: function () {
            '<div class="infoMessage"></div>';
            var infoMsg = $("<div class='infoMessage'>").text("Начните вводить текст чтобы выбрать значение");
            infoMsg.insertAfter(this.input.parent());            
        },

        _createAutocomplete: function () {
            var selected = this.element.children(":selected"),
              value = selected.val() ? selected.text() : "";

            this.input = $("<input>") // $("<input placeholder=' - не выбрано - '>")
              .appendTo(this.wrapper)
              .val(value)
              .attr("title", "")
              .addClass("form-control custom-combobox-input ui-widget ui-widget-content ui-state-default ui-corner-left")
              .autocomplete({
                  delay: 0,
                  minLength: 0,
                  source: $.proxy(this, "_source")
              })
              .focus(function(event){
                  if ($(this).val() != '') {
                      $(this).autocomplete("search", "");
                      var menu = $(this).data("uiAutocomplete").menu.element,
                      focused = menu.find("li:contains('" + $(this).val() + "')");
                      if (focused && focused.length ==1)
                          focused.addClass('ui-state-focus');
                  }
              })
             
            this._on(this.input, {
                autocompleteselect: function (event, ui) {
                    ui.item.option.selected = true;                    
                    this._trigger("select", event, {
                        item: ui.item.option
                    });
                    this.element.change();
                },

                autocompletechange: "_removeIfInvalid",                
            });
        },

        /* initialize the full list of items, this menu will be reused whenever the user clicks the show all button */
        _renderFullMenu: function (source) {
            var menu = $(this).data("uiAutocomplete").menu.element,
                    focused = menu.find("li:contains('" + $(this).val() + "')");

            var self = this,
                input = this.input,
                ul = input.data("uiAutocomplete").menu.element,
                lis = [];
            source = this._normalize(source);
            input.data("uiAutocomplete").menuAll = input.data("uiAutocomplete").menu.element.clone(true).appendTo("body");
            for (var i = 0; i < source.length; i++) {
                lis[i] = "<li class=\"ui-menu-item\" role=\"menuitem\"><a class=\"ui-corner-all\" tabindex=\"-1\">" + source[i].label + "</a></li>";
            }
            ul.append(lis.join(""));
            this._resizeMenu();
            // setup the rest of the data, and event stuff
            setTimeout(function () {
                self._setupMenuItem.call(self, ul.children("li"), source);
            }, 0);
            input.isFullMenu = true;
        },

        _createShowAllButton: function () {
            var input = this.input,
                wasOpen = false;

            $("<span role='button' ><span class='ui-button-icon ui-icon ui-icon-triangle-1-s'></span><span class='ui-button-icon-space'> </span></span>")
              .attr("tabIndex", -1)
              .attr("title", "Показать все элементы")
              .tooltip()
              .appendTo(this.wrapper)
              .removeClass("ui-corner-all")
              .addClass("input-group-addon ui-button ui-widget custom-combobox-toggle")
              .on("mousedown", function () {
                  wasOpen = input.autocomplete("widget").is(":visible");
              })
              .on("click", function () {
                  input.trigger("focus");

                  // Close if already visible
                  if (wasOpen) {
                      return;
                  }

                  // Pass empty string as value to search for, displaying all results
                  input.autocomplete("search", "");
              });
        },

        _source: function (request, response) {
            var matcher = new RegExp($.ui.autocomplete.escapeRegex(request.term), "i");
            response(this.element.children("option").map(function () {
                var text = $(this).text();
                if (this.value && (!request.term || matcher.test(text)))
                    return {
                        label: text,
                        value: text,
                        option: this
                    };
            }));
        },

        _removeIfInvalid: function (event, ui) {

            // Selected an item, nothing to do
            if (ui.item) {
                return;
            }

            // Search for a match (case-insensitive)
            var value = this.input.val(),
              valueLowerCase = value.toLowerCase(),
              valid = false;
            this.element.children("option").each(function () {
                if ($(this).text().toLowerCase() === valueLowerCase) {
                    this.selected = valid = true;
                    return false;
                }
            });

            // Found a match, nothing to do
            if (valid) {
                return;
            }

            // Remove invalid value         
            this.input.autocomplete("instance").term = "";
            this.element.val("");            
        },

        _destroy: function () {
            this.wrapper.remove();
            this.element.show();
        }
    });    
    
    $.widget("custom.searchcombobox", {
        _create: function () {
            this.wrapper = $("<div>")
              .addClass("input-group custom-combobox")
              .insertAfter(this.element);

            this.element.hide();
            this.element.attr('disabled', 'disabled');
            this._createAutocomplete();
            if (this.element.get(0).length > SLOW_LIMIT) {
                this.input.data("uiAutocomplete").options.minLength = MIN_LENGTH;
                this._createInfoMessage();
            }
        },

        _createInfoMessage: function () {
            '<div class="infoMessage"></div>';
            var infoMsg = $("<div class='infoMessage'>").text("Начните вводить текст чтобы выбрать значение");
            infoMsg.insertAfter(this.input.parent());
        },

        _createAutocomplete: function () {
            var selected = this.element.children(":selected"),
              value = selected.val() ? selected.text() : "";

            var pWidth = this.element.css("width");
            var iName = this.element.attr("name").replace("Combo", "");
            this.input = $("<input>") // $("<input placeholder=' - не выбрано - '>")
              .appendTo(this.wrapper)
              .val(value)
              .attr("title", "")
              .attr("name", "searchString")
              .css("width", pWidth)
              .css('margin-top', '1px')
              .addClass("form-control custom-combobox-input ui-widget ui-widget-content ui-state-default ui-corner-left")
              .autocomplete({
                  delay: 0,
                  minLength: 0,
                  source: $.proxy(this, "_source")
              })
              .focus(function (event) {
                  if ($(this).val() != '') {
                      $(this).autocomplete("search", "");
                      var menu = $(this).data("uiAutocomplete").menu.element,
                      focused = menu.find("li:contains('" + $(this).val() + "')");
                      if (focused && focused.length == 1)
                          focused.addClass('ui-state-focus');
                  }
              })
            .keyup(function (event) {
                if (event.keyCode == 13)
                    $('form').submit();
            });
            

            var btnSearch = $('<span class="btn btn-default glyphicon glyphicon-search"></span>')
                .css('margin-left', '-2px')
                .css('border-left-style','hidden')
            .click(function (event) {
                $('form').submit();
            });
            btnSearch.insertAfter(this.input);
            this._on(this.input, {
                autocompleteselect: function (event, ui) {
                    ui.item.option.selected = true;
                    this._trigger("select", event, {
                        item: ui.item.option
                    });
                    this.input.val(ui.item.option.text);
                    $('form').submit();
                },

                autocompletechange: "_removeIfInvalid",
            });
        },

        /* initialize the full list of items, this menu will be reused whenever the user clicks the show all button */
        _renderFullMenu: function (source) {
            var menu = $(this).data("uiAutocomplete").menu.element,
                    focused = menu.find("li:contains('" + $(this).val() + "')");

            var self = this,
                input = this.input,
                ul = input.data("uiAutocomplete").menu.element,
                lis = [];
            source = this._normalize(source);
            input.data("uiAutocomplete").menuAll = input.data("uiAutocomplete").menu.element.clone(true).appendTo("body");
            for (var i = 0; i < source.length; i++) {
                lis[i] = "<li class=\"ui-menu-item\" role=\"menuitem\"><a class=\"ui-corner-all\" tabindex=\"-1\">" + source[i].label + "</a></li>";
            }
            ul.append(lis.join(""));
            this._resizeMenu();
            // setup the rest of the data, and event stuff
            setTimeout(function () {
                self._setupMenuItem.call(self, ul.children("li"), source);
            }, 0);
            input.isFullMenu = true;
        },
       
        _source: function (request, response) {
            var matcher = new RegExp($.ui.autocomplete.escapeRegex(request.term), "i");
            response(this.element.children("option").map(function () {
                var text = $(this).text();
                if (this.value && (!request.term || matcher.test(text)))
                    return {
                        label: text,
                        value: text,
                        option: this
                    };
            }));
        },

        _removeIfInvalid: function (event, ui) {

            // Selected an item, nothing to do
            if (ui.item) {
                return;
            }

            // Search for a match (case-insensitive)
            var value = this.input.val(),
              valueLowerCase = value.toLowerCase(),
              valid = false;
            this.element.children("option").each(function () {
                if ($(this).text().toLowerCase() === valueLowerCase) {
                    this.selected = valid = true;
                    return false;
                }
            });

            // Found a match, nothing to do
            if (valid) {
                return;
            }

            // Remove invalid value         
            this.input.autocomplete("instance").term = "";
            this.element.val("");
        },

        _destroy: function () {
            this.wrapper.remove();
            this.element.show();
        }
    });

});

function SetPostbackValue(searchInput, value)
{
    if (value != undefined && value != null && value != "")
    {
        $('input[name="' + searchInput + '"]').val(decodeURIComponent(value));
    }
}