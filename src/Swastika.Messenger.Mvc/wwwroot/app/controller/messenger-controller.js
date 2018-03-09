'use strict';
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

    $scope.connection = null;

    $scope.user = {
        userId: '',
        userName: '',
        avatar: '',
        connectionId: '',
        message:'',
        isOnline: false
    };
    $scope.range = function (max) {
        var input = [];
        for (var i = 1; i <= max; i += 1) input.push(i);
        return input;
    };

    $scope.connect = function () {
        $scope.connection.invoke('hubConnect', $scope.user);
    };

    // Starts a connection with transport fallback - if the connection cannot be started using
    // the webSockets transport the function will fallback to the serverSentEvents transport and
    // if this does not work it will try longPolling. If the connection cannot be started using
    // any of the available transports the function will return a rejected Promise.
    $scope.startConnection = function (url) {
        return function start(transport) {
            console.log(`Starting connection using ${signalR.TransportType[transport]} transport`)
            $scope.connection = new signalR.HubConnection(url, { transport: transport });

            // Create a function that the hub can call to broadcast messages.
            $scope.connection.on('broadcastMessage', function (name, message) {
                // Html encode display name and message.
                $scope.user.userName = name;
                var encodedMsg = message;
                // Add the message to the page.
                var liElement = document.createElement('li');
                liElement.innerHTML = '<strong>' + $scope.user.userName + '</strong>:&nbsp;&nbsp;' + encodedMsg;
                document.getElementById('discussion').appendChild(liElement);
            });
            $scope.connection.on('receiveMessage', function (data) {
                console.log(data);
                if (data.isSucceed) {
                    switch (data.responseKey) {
                        case 'Connect':
                            $scope.user.isOnline = true;
                            $scope.$apply();
                            return false;
                        default:
                    }

                }
            });

            return $scope.connection.start()
                .then(function () {                    
                    console.log('connection started');
                    
                    $scope.user.connectionId = $scope.connection.connection.connectionId;
                    document.getElementById('sendmessage').addEventListener('click', function (event) {
                        // Call the Send method on the hub.
                        $scope.connection.invoke('send', $scope.user.userName, $scope.user.message);
                        // Clear text box and reset focus for next comment.
                        $scope.user.message = '';
                        event.preventDefault();
                    });
                    //$scope.$apply();
                })
                .catch(function (error) {
                    console.log(`Cannot start the connection use ${signalR.TransportType[transport]} transport. ${error.message}`);
                    if (transport !== signalR.TransportType.LongPolling) {
                        return start(transport + 1);
                    }
                    return Promise.reject(error);
                });
        }(signalR.TransportType.LongPolling);

        //return function start(transport) {
        //    console.log(`Starting connection using ${signalR.TransportType[transport]} transport`)
        //    $scope.connection = new signalR.HubConnection(url, { transport: transport });
        //    if (configureConnection && typeof configureConnection === 'function') {
        //        configureConnection();
        //    }
        //    return $scope.connection.start()
        //        .then(function () {
        //            console.log('connection started');
        //            $scope.user.isOnline = true;
        //            $scope.connection.invoke('hubConnect', $scope.user);

        //            document.getElementById('sendmessage').addEventListener('click', function (event) {
        //                // Call the Send method on the hub.
        //                $scope.connection.invoke('send', name, messageInput.value);
        //                // Clear text box and reset focus for next comment.
        //                messageInput.value = '';
        //                messageInput.focus();
        //                event.preventDefault();
        //            });
        //        })
        //        .catch(function (error) {
        //            console.log(`Cannot start the connection use ${signalR.TransportType[transport]} transport. ${error.message}`);
        //            if (transport !== signalR.TransportType.LongPolling) {
        //                return start(transport + 1);
        //            }
        //            return Promise.reject(error);
        //        });
        //}(signalR.TransportType.LongPolling);
    };


});
