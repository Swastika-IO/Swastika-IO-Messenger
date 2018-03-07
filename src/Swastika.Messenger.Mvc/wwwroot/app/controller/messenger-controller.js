﻿'use strict';
appMessenger.controller('MessengerController', function PhoneListController($scope) {
    $scope.currentLanguage = $('#curentLanguage').val();
    $scope.isBusy = true;
    $scope.data = [];
    $scope.orders = [
        {
            value: 'CreatedDateTime',
            title: 'Created Date'
        }
        ,
        {
            value: 'Priority',
            title: 'Priority'
        },

        {
            value: 'Title',
            title: 'Title'
        }
    ];
    $scope.directions = [
        {
            value: '0',
            title: 'Asc'
        },
        {
            value: '1',
            title: 'Desc'
        }
    ]
    $scope.pageSizes = [
        '5',
        '10',
        '15',
        '20'
    ]
    $scope.request = {
        pageSize: '10',
        pageIndex: 0,
        orderBy: 'CreatedDateTime',
        direction: '0',
        fromDate: null,
        toDate: null,
        keyword: ''
    };
    $scope.settings = {
        async: true,
        crossDomain: true,
        url: "",
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded"
        },
        data: $scope.request
    };

    $scope.range = function (max) {
        var input = [];
        for (var i = 1; i <= max; i += 1) input.push(i);
        return input;
    };

    $scope.loadMedia = function (pageIndex) {
        if (pageIndex != undefined) {
            $scope.request.pageIndex = pageIndex;
        }
        var url = '/api/' + $scope.currentLanguage + '/media/list';//byProduct/' + productId;
        $scope.settings.url = url;// + '/true';
        $scope.settings.data = $scope.request;
        $.ajax($scope.settings).done(function (response) {
            $scope.$apply($scope.mediaData = response.data);

            $.each($scope.mediaData.items, function (i, media) {
                $.each($scope.activedMedias, function (i, e) {
                    if (e.mediaId == media.id) {
                        media.isHidden = true;
                    }
                })
            })
        });
    };
    $scope.uploadMedia = function () {
        //var container = $(this).parents('.model-media').first().find('.custom-file').first();
        if ($scope.mediaFile.file !== undefined && $scope.mediaFile.file !== null) {
            // Create FormData object
            var files = new FormData();

            // Looping over all files and add it to FormData object
            files.append($scope.mediaFile.file.name, $scope.mediaFile.file);

            // Adding one more key to FormData object
            files.append('fileFolder', $scope.mediaFile.folder);
            files.append('title', $scope.mediaFile.title);
            files.append('description', $scope.mediaFile.description);
            $.ajax({
                url: '/api/' + $scope.currentLanguage + '/media/upload', //'/api/tts/UploadImage',
                type: "POST",
                contentType: false, // Not to set any content header
                processData: false, // Not to process data
                data: files,
                success: function (result) {
                    if (result.isSucceed) {
                        $scope.mediaFile.file = null;
                        $scope.loadMedia();
                        $('.upload-image-modal-lg').modal('hide');
                        return result;
                    }
                },
                error: function (err) {
                    return '';
                }
            });
        }
    };

    $scope.removeMedia = function (mediaId) {
        if (confirm("Are you sure!")) {
            var url = '/api/' + $scope.currentLanguage + '/media/delete/' + mediaId;
            $.ajax({
                method: 'GET',
                url: url,
                success: function (data) {
                    $scope.loadMedia();
                },
                error: function (a, b, c) {
                    console.log(a + " " + b + " " + c);
                }
            });
        }
    };
    $scope.saveMedia = function (media) {
        var url = '/api/' + $scope.currentLanguage + '/media/save';
        $.ajax({
            method: 'POST',
            url: url,
            data: media,
            success: function (data) {
                //$scope.loadMedia();
                if (data.isSucceed) {
                    alert('success');
                }
                else {
                    alert('failed! ' + data.errors);
                }
            },
            error: function (a, b, c) {
                console.log(a + " " + b + " " + c);
            }
        });
    };

    $scope.loadProduct = function (pageIndex = 0, pageSize = 16, orderBy = 'title', direction = 0) {
        var request = {
            "pageSize": pageSize,
            "pageIndex": pageIndex,
            "orderBy": orderBy,
            "direction": direction,
            "keyword": $('#keyword').val()
        }
        var url = '/api/' + $scope.currentLanguage + '/product/list';//byProduct/' + productId;
        $scope.settings.url = url;// + '/true';
        $scope.settings.data = request;
        $.ajax($scope.settings).done(function (response) {
            $scope.productData = response.data;
        });
    };

    $scope.changeMedia = function (media) {
        var currentItem = null;
        if ($scope.activedMedias == undefined) {
            $scope.activedMedias = [];
        }
        $.each($scope.activedMedias, function (i, e) {
            if (e.mediaId == media.id) {
                e.isActived = media.isActived;
                currentItem = e;
                return false;
            }
        });
        if (currentItem == null) {
            currentItem = {
                description: media.description,
                image: media.fullPath,
                mediaId: media.id,
                product: $('#product-id').val(),
                specificulture: media.specificulture,
                position: 0,
                priority: $scope.activedMedias.length + 1,
                isActived: true
            };
            media.isHidden = true;
            $scope.activedMedias.push(currentItem);
        }
    }

    $scope.changeProduct = function (product) {
        var currentItem = null;
        $.each($scope.activedProducts, function (i, e) {
            if (e.relatedProductId == product.id) {
                e.isActived = product.isActived;
                currentItem = e;
                return false;
            }
        });
        if (currentItem == null) {
            currentItem = {
                relatedProductId: product.id,
                sourceProductId: $('#product-id').val(),
                specificulture: product.specificulture,
                priority: $scope.activedMedias.length + 1,
                product: product,
                isActived: true
            };
            product.isHidden = true;
            $scope.activedProducts.push(currentItem);
        }
    }
    $scope.addProperty = function (type) {
        var i = $(".property").length;
        $.ajax({
            method: 'GET',
            url: '/' + $scope.currentLanguage + '/Portal/' + type + '/AddEmptyProperty/' + i,
            success: function (data) {
                $('#tbl-properties > tbody').append(data);
                $(data).find('.prop-data-type').trigger('change');
            },
            error: function (a, b, c) {
                console.log(a + " " + b + " " + c);
            }
        });
    }


    $scope.updateEditors = function () {
        setTimeout(function () {
            $.each($('.code-editor'), function (i, e) {
                var container = $(this);
                var editor = ace.edit(e);
                var val = $(this).next('input').val();
                editor.setValue(val);
                if (container.hasClass('json')) {
                    editor.session.setMode("ace/mode/json");
                }
                else {
                    editor.session.setMode("ace/mode/razor");
                }
                editor.setTheme("ace/theme/chrome");
                //editor.setReadOnly(true);

                editor.session.setUseWrapMode(true);
                editor.setOptions({
                    maxLines: Infinity
                });
                editor.getSession().on('change', function (e) {
                    // e.type, etc
                    $(container).parent().find('.code-content').val(editor.getValue());
                });
            });
        }, 200);
    };
});
